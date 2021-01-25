// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated1269 { .hash algorithm 0x00008004 }
.module Generated1269.exe

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
.class public G3_C1741`1<T0> 
		extends class G2_C705`2<!T0,class BaseClass0>
{
	.method public hidebysig newslot virtual instance string ClassMethod4880() cil managed noinlining { 
		ldstr "G3_C1741::ClassMethod4880.17738()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4881<M0>() cil managed noinlining { 
		ldstr "G3_C1741::ClassMethod4881.17739<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4882<M0>() cil managed noinlining { 
		ldstr "G3_C1741::ClassMethod4882.17740<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C705<T0,class BaseClass0>.ClassMethod2771'() cil managed noinlining { 
		.override method instance string class G2_C705`2<!T0,class BaseClass0>::ClassMethod2771()
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C705<T0,class BaseClass0>.ClassMethod2774'<M0>() cil managed noinlining { 
		.override method instance string class G2_C705`2<!T0,class BaseClass0>::ClassMethod2774<[1]>()
		ldstr "G3_C1741::ClassMethod2774.MI.17742<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C705`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C705`2<T0, T1> 
		extends class G1_C13`2<class BaseClass1,class BaseClass1>
		implements class IBase2`2<class BaseClass1,!T0>, IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C705::Method7.11496<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C705::Method0.11497()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C705::Method1.11499()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C705::Method2.11501<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C705::Method3.11502<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2771() cil managed noinlining { 
		ldstr "G2_C705::ClassMethod2771.11503()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2772() cil managed noinlining { 
		ldstr "G2_C705::ClassMethod2772.11504()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2773<M0>() cil managed noinlining { 
		ldstr "G2_C705::ClassMethod2773.11505<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2774<M0>() cil managed noinlining { 
		ldstr "G2_C705::ClassMethod2774.11506<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C13`2<class BaseClass1,class BaseClass1>::.ctor()
		ret
	}
}
.class public abstract G1_C13`2<T0, T1> 
		implements class IBase2`2<!T0,!T1>, class IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method7.4871<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C13::Method4.4872()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C13::Method5.4873()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method6.4875<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1348<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1348.4876<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1349<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1349.4877<"
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
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
	.method static void M.G3_C1741.T<T0,(class G3_C1741`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 22
		.locals init (string[] actualResults)
		ldc.i4.s    17
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1741.T<T0,(class G3_C1741`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 17
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod4880()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod4881<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::ClassMethod4882<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1741.A<(class G3_C1741`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 22
		.locals init (string[] actualResults)
		ldc.i4.s    17
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1741.A<(class G3_C1741`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 17
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod4880()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod4881<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::ClassMethod4882<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1741.B<(class G3_C1741`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 22
		.locals init (string[] actualResults)
		ldc.i4.s    17
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1741.B<(class G3_C1741`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 17
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod4880()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod4881<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::ClassMethod4882<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1741`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.T.T<T0,T1,(class G2_C705`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.T.T<T0,T1,(class G2_C705`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.A.T<T1,(class G2_C705`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.A.T<T1,(class G2_C705`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.A.A<(class G2_C705`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.A.A<(class G2_C705`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.A.B<(class G2_C705`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.A.B<(class G2_C705`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.B.T<T1,(class G2_C705`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.B.T<T1,(class G2_C705`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.B.A<(class G2_C705`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.B.A<(class G2_C705`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C705.B.B<(class G2_C705`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C705.B.B<(class G2_C705`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2771()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2772()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2773<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2774<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.T.T<T0,T1,(class G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class G1_C13`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class G1_C13`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class G1_C13`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class G1_C13`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class G1_C13`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class G1_C13`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class G1_C13`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class G1_C13`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class G1_C13`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class G1_C13`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class G1_C13`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class G1_C13`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1741`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2774<object>()
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2771()
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod4882<object>()
		ldstr "G3_C1741::ClassMethod4882.17740<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod4881<object>()
		ldstr "G3_C1741::ClassMethod4881.17739<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod4880()
		ldstr "G3_C1741::ClassMethod4880.17738()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod2774<object>()
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod2771()
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass0>
		callvirt instance string class G3_C1741`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1741`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2774<object>()
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2771()
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod4882<object>()
		ldstr "G3_C1741::ClassMethod4882.17740<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod4881<object>()
		ldstr "G3_C1741::ClassMethod4881.17739<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod4880()
		ldstr "G3_C1741::ClassMethod4880.17738()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod2774<object>()
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod2771()
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1741`1<class BaseClass1>
		callvirt instance string class G3_C1741`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C705`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2774<object>()
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2771()
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C705`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2774<object>()
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2771()
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C705`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2774<object>()
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2771()
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C705`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2774<object>()
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2773<object>()
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2772()
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2771()
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1741`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.T.T<class BaseClass1,class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.T<class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.B<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.T.T<class BaseClass0,class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.A.T<class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.A.A<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method0.MI.11498()#G2_C705::Method1.MI.11500()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1741`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G3_C1741::ClassMethod4880.17738()#G3_C1741::ClassMethod4881.17739<System.Object>()#G3_C1741::ClassMethod4882.17740<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G3_C1741.T<class BaseClass0,class G3_C1741`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G3_C1741::ClassMethod4880.17738()#G3_C1741::ClassMethod4881.17739<System.Object>()#G3_C1741::ClassMethod4882.17740<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G3_C1741.A<class G3_C1741`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1741`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.T.T<class BaseClass1,class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.T<class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.B<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.T.T<class BaseClass1,class BaseClass0,class G3_C1741`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.B.T<class BaseClass0,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.B.A<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method0.MI.11498()#G2_C705::Method1.MI.11500()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1741`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G3_C1741::ClassMethod4880.17738()#G3_C1741::ClassMethod4881.17739<System.Object>()#G3_C1741::ClassMethod4882.17740<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G3_C1741.T<class BaseClass1,class G3_C1741`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1741::ClassMethod2771.MI.17741()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G3_C1741::ClassMethod2774.MI.17742<System.Object>()#G3_C1741::ClassMethod4880.17738()#G3_C1741::ClassMethod4881.17739<System.Object>()#G3_C1741::ClassMethod4882.17740<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G3_C1741.B<class G3_C1741`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C705`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.B<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.T.T<class BaseClass0,class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.A.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.A.A<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method0.MI.11498()#G2_C705::Method1.MI.11500()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#"
		call void Framework::M.IBase0<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C705`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C705`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.B<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.T.T<class BaseClass0,class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.A.T<class BaseClass1,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.A.B<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method0.MI.11498()#G2_C705::Method1.MI.11500()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#"
		call void Framework::M.IBase0<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C705`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C705`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C705`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.B<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.T.T<class BaseClass1,class BaseClass0,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.B.T<class BaseClass0,class G2_C705`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.B.A<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method0.MI.11498()#G2_C705::Method1.MI.11500()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#"
		call void Framework::M.IBase0<class G2_C705`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C705`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G1_C13.B.B<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::Method4.4872()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.T.T<class BaseClass1,class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.B.T<class BaseClass1,class G2_C705`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C705::ClassMethod2771.11503()#G2_C705::ClassMethod2772.11504()#G2_C705::ClassMethod2773.11505<System.Object>()#G2_C705::ClassMethod2774.11506<System.Object>()#G2_C705::Method0.11497()#G2_C705::Method1.11499()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#G1_C13::Method4.4872()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G2_C705::Method7.11496<System.Object>()#"
		call void Framework::M.G2_C705.B.B<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C705::Method0.MI.11498()#G2_C705::Method1.MI.11500()#G2_C705::Method2.11501<System.Object>()#G2_C705::Method3.11502<System.Object>()#"
		call void Framework::M.IBase0<class G2_C705`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1741`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2774<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2773<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2772()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2771()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod4882<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod4882.17740<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod4881<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod4881.17739<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod4880()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod4880.17738()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod2774<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod2773<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod2772()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod2771()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method1()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method0()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method5()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass0>::Method4()
		calli default string(class G3_C1741`1<class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1741`1<class BaseClass0> on type class G3_C1741`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1741`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2774<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2773<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2772()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2771()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod4882<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod4882.17740<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod4881<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod4881.17739<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod4880()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod4880.17738()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod2774<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod2774.MI.17742<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod2773<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod2772()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod2771()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G3_C1741::ClassMethod2771.MI.17741()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method1()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method0()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method5()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1741`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1741`1<class BaseClass1>::Method4()
		calli default string(class G3_C1741`1<class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G3_C1741`1<class BaseClass1> on type class G3_C1741`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C705`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2774<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2773<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2772()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod2771()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C705`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2774<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2773<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2772()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod2771()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C705`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C705`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2774<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2773<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2772()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod2771()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C705`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C13`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C13`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G1_C13`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2774<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2774.11506<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2773<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2773.11505<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2772()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2772.11504()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod2771()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::ClassMethod2771.11503()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method1.11499()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method0.11497()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method7.11496<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod1349<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::ClassMethod1348<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::ClassMethod1348.4876<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C705`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C705`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G2_C705`2<class BaseClass1,class BaseClass1> on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method0.MI.11498()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method1.MI.11500()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method2.11501<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C705`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C705::Method3.11502<System.Object>()"
		ldstr "IBase0 on type class G2_C705`2<class BaseClass1,class BaseClass1>"
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
