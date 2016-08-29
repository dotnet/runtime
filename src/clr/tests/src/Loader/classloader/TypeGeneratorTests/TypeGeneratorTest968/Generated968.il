// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated968 { .hash algorithm 0x00008004 }
.module Generated968.exe

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
.class public G3_C1440`1<T0> 
		extends class G2_C471`2<!T0,class BaseClass0>
		implements class IBase2`2<!T0,class BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1440::Method7.16190<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4268() cil managed noinlining { 
		ldstr "G3_C1440::ClassMethod4268.16191()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4269() cil managed noinlining { 
		ldstr "G3_C1440::ClassMethod4269.16192()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4270<M0>() cil managed noinlining { 
		ldstr "G3_C1440::ClassMethod4270.16193<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C471<T0,class BaseClass0>.ClassMethod2300'() cil managed noinlining { 
		.override method instance string class G2_C471`2<!T0,class BaseClass0>::ClassMethod2300()
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C471<T0,class BaseClass0>.ClassMethod2302'<M0>() cil managed noinlining { 
		.override method instance string class G2_C471`2<!T0,class BaseClass0>::ClassMethod2302<[1]>()
		ldstr "G3_C1440::ClassMethod2302.MI.16195<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C471`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C471`2<T0, T1> 
		extends class G1_C9`2<class BaseClass0,class BaseClass0>
		implements class IBase2`2<class BaseClass1,!T1>, class IBase1`1<class BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C471::Method7.9269<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C471::Method4.9270()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C471::Method5.9271()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C471::Method6.9273<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass0>::Method6<[1]>()
		ldstr "G2_C471::Method6.MI.9274<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2300() cil managed noinlining { 
		ldstr "G2_C471::ClassMethod2300.9275()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2301() cil managed noinlining { 
		ldstr "G2_C471::ClassMethod2301.9276()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2302<M0>() cil managed noinlining { 
		ldstr "G2_C471::ClassMethod2302.9277<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2303<M0>() cil managed noinlining { 
		ldstr "G2_C471::ClassMethod2303.9278<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class BaseClass0,class BaseClass0>.ClassMethod1336'() cil managed noinlining { 
		.override method instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class BaseClass0,class BaseClass0>.ClassMethod1337'() cil managed noinlining { 
		.override method instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C9`2<class BaseClass0,class BaseClass0>::.ctor()
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
	.method static void M.G3_C1440.T<T0,(class G3_C1440`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1440.T<T0,(class G3_C1440`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod4268()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod4269()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::ClassMethod4270<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1440.A<(class G3_C1440`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1440.A<(class G3_C1440`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod4268()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod4269()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::ClassMethod4270<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1440.B<(class G3_C1440`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1440.B<(class G3_C1440`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod4268()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod4269()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::ClassMethod4270<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1440`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.T.T<T0,T1,(class G2_C471`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.T.T<T0,T1,(class G2_C471`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.A.T<T1,(class G2_C471`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.A.T<T1,(class G2_C471`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.A.A<(class G2_C471`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.A.A<(class G2_C471`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.A.B<(class G2_C471`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.A.B<(class G2_C471`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.B.T<T1,(class G2_C471`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.B.T<T1,(class G2_C471`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.B.A<(class G2_C471`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.B.A<(class G2_C471`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C471.B.B<(class G2_C471`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C471.B.B<(class G2_C471`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2300()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2301()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2302<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2303<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1440`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2302<object>()
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2300()
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod4270<object>()
		ldstr "G3_C1440::ClassMethod4270.16193<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod4269()
		ldstr "G3_C1440::ClassMethod4269.16192()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod4268()
		ldstr "G3_C1440::ClassMethod4268.16191()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod2302<object>()
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod2300()
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass0>
		callvirt instance string class G3_C1440`1<class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1440`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2302<object>()
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2300()
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod4270<object>()
		ldstr "G3_C1440::ClassMethod4270.16193<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod4269()
		ldstr "G3_C1440::ClassMethod4269.16192()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod4268()
		ldstr "G3_C1440::ClassMethod4268.16191()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod2302<object>()
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod2300()
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1440`1<class BaseClass1>
		callvirt instance string class G3_C1440`1<class BaseClass1>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C471`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2302<object>()
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2300()
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C471`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2302<object>()
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2300()
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C471`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2302<object>()
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2300()
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C471`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2303<object>()
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2302<object>()
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2301()
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2300()
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1440`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1440`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.T.T<class BaseClass0,class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.A.T<class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.A.A<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1440`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1440`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G3_C1440::ClassMethod4268.16191()#G3_C1440::ClassMethod4269.16192()#G3_C1440::ClassMethod4270.16193<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.G3_C1440.T<class BaseClass0,class G3_C1440`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G3_C1440::ClassMethod4268.16191()#G3_C1440::ClassMethod4269.16192()#G3_C1440::ClassMethod4270.16193<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.G3_C1440.A<class G3_C1440`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1440`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1440`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.T.T<class BaseClass1,class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.B.T<class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.B.A<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1440`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1440`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G3_C1440::ClassMethod4268.16191()#G3_C1440::ClassMethod4269.16192()#G3_C1440::ClassMethod4270.16193<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.G3_C1440.T<class BaseClass1,class G3_C1440`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G3_C1440::ClassMethod2300.MI.16194()#G2_C471::ClassMethod2301.9276()#G3_C1440::ClassMethod2302.MI.16195<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G3_C1440::ClassMethod4268.16191()#G3_C1440::ClassMethod4269.16192()#G3_C1440::ClassMethod4270.16193<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G3_C1440::Method7.16190<System.Object>()#"
		call void Framework::M.G3_C1440.B<class G3_C1440`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C471`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.A.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.A.A<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C471`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C471`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G2_C471`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C471`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C471`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C471`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.T.T<class BaseClass0,class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.A.T<class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.A.B<class G2_C471`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C471`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C471`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C471`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.T.T<class BaseClass1,class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.B.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.B.A<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C471`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C471`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G1_C9::Method6.4838<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G1_C9.A.A<class G2_C471`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C471`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method4.9270()#G2_C471::Method5.MI.9272()#G2_C471::Method6.MI.9274<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C471`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C471`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.T.T<class BaseClass1,class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.B.T<class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::ClassMethod1336.MI.9279()#G2_C471::ClassMethod1337.MI.9280()#G1_C9::ClassMethod1338.4842<System.Object>()#G1_C9::ClassMethod1339.4843<System.Object>()#G2_C471::ClassMethod2300.9275()#G2_C471::ClassMethod2301.9276()#G2_C471::ClassMethod2302.9277<System.Object>()#G2_C471::ClassMethod2303.9278<System.Object>()#G2_C471::Method4.9270()#G2_C471::Method5.9271()#G2_C471::Method6.9273<System.Object>()#G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.G2_C471.B.B<class G2_C471`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C471`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C471::Method7.9269<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C471`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1440`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2303<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2302<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2301()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2300()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod4270<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod4270.16193<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod4269()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod4269.16192()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod4268()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod4268.16191()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod2303<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod2302<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod2301()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod2300()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1440`1<class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G3_C1440`1<class BaseClass0> on type class G3_C1440`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1440`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2303<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2302<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2301()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2300()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod4270<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod4270.16193<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod4269()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod4269.16192()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod4268()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod4268.16191()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::Method7.16190<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod2303<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod2302<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod2302.MI.16195<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod2301()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod2300()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G3_C1440::ClassMethod2300.MI.16194()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::Method5()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::Method4()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod1337()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1440`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1440`1<class BaseClass1>::ClassMethod1336()
		calli default string(class G3_C1440`1<class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G3_C1440`1<class BaseClass1> on type class G3_C1440`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C471`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2303<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2302<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2301()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod2300()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C471`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2303<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2302<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2301()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod2300()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass0,class BaseClass1>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C471`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2303<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2302<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2301()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod2300()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C471`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C9`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C9`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G1_C9`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method5.MI.9272()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method6.MI.9274<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2303<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2303.9278<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2302<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2302.9277<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2301()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2301.9276()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod2300()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod2300.9275()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method6.9273<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method5.9271()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method4.9270()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1339<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1339.4843<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1338<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C9::ClassMethod1338.4842<System.Object>()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1337()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1337.MI.9280()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C471`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C471`2<class BaseClass1,class BaseClass1>::ClassMethod1336()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::ClassMethod1336.MI.9279()"
		ldstr "class G2_C471`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C471`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C471::Method7.9269<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C471`2<class BaseClass1,class BaseClass1>"
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
