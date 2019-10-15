// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated931 { .hash algorithm 0x00008004 }
.module Generated931.exe

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
.class public G3_C1403`1<T0> 
		extends class G2_C430`2<class BaseClass0,!T0>
		implements class IBase1`1<class BaseClass1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1403::Method4.15989()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1403::Method4.MI.15990()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1403::Method5.15991()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1403::Method5.MI.15992()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1403::Method6.15993<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4200() cil managed noinlining { 
		ldstr "G3_C1403::ClassMethod4200.15994()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4201() cil managed noinlining { 
		ldstr "G3_C1403::ClassMethod4201.15995()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C430<class BaseClass0,T0>.ClassMethod2222'<M0>() cil managed noinlining { 
		.override method instance string class G2_C430`2<class BaseClass0,!T0>::ClassMethod2222<[1]>()
		ldstr "G3_C1403::ClassMethod2222.MI.15996<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C430`2<class BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public G2_C430`2<T0, T1> 
		extends class G1_C8`2<class BaseClass1,!T0>
		implements IBase0, class IBase2`2<!T1,class BaseClass0> 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C430::Method0.8866()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C430::Method1.8868()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C430::Method2.8870<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C430::Method2.MI.8871<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C430::Method3.8872<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C430::Method7.8873<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2219() cil managed noinlining { 
		ldstr "G2_C430::ClassMethod2219.8874()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2220() cil managed noinlining { 
		ldstr "G2_C430::ClassMethod2220.8875()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2221<M0>() cil managed noinlining { 
		ldstr "G2_C430::ClassMethod2221.8876<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2222<M0>() cil managed noinlining { 
		ldstr "G2_C430::ClassMethod2222.8877<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass1,T0>.ClassMethod1332'() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass1,!T0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass1,T0>.ClassMethod1333'() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass1,!T0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass1,T0>.ClassMethod1334'<M0>() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass1,!T0>::ClassMethod1334<[1]>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass1,T0>.ClassMethod1335'<M0>() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass1,!T0>::ClassMethod1335<[1]>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C8`2<class BaseClass1,!T0>::.ctor()
		ret
	}
}
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public G1_C8`2<T0, T1> 
		implements class IBase2`2<!T0,!T1>, IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C8::Method7.4821<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,!T1>::Method7<[1]>()
		ldstr "G1_C8::Method7.MI.4822<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G1_C8::Method0.4823()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G1_C8::Method1.4825()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G1_C8::Method2.4826<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G1_C8::Method2.MI.4827<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G1_C8::Method3.4828<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1332() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1332.4829()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1333() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1333.4830()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1334<M0>() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1334.4831<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1335<M0>() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1335.4832<"
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
	.method static void M.G3_C1403.T<T0,(class G3_C1403`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1403.T<T0,(class G3_C1403`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod4200()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::ClassMethod4201()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1403.A<(class G3_C1403`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1403.A<(class G3_C1403`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod4200()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::ClassMethod4201()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1403.B<(class G3_C1403`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 23
		.locals init (string[] actualResults)
		ldc.i4.s    18
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1403.B<(class G3_C1403`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 18
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod4200()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::ClassMethod4201()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     15
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     16
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     17
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1403`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.T.T<T0,T1,(class G2_C430`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.T.T<T0,T1,(class G2_C430`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.A.T<T1,(class G2_C430`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.A.T<T1,(class G2_C430`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.A.A<(class G2_C430`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.A.A<(class G2_C430`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.A.B<(class G2_C430`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.A.B<(class G2_C430`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.B.T<T1,(class G2_C430`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.B.T<T1,(class G2_C430`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.B.A<(class G2_C430`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.B.A<(class G2_C430`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C430.B.B<(class G2_C430`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C430.B.B<(class G2_C430`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2219()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2220()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2221<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2222<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C8.T.T<T0,T1,(class G1_C8`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.T.T<T0,T1,(class G1_C8`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.A.T<T1,(class G1_C8`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.A.T<T1,(class G1_C8`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.A.A<(class G1_C8`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.A.A<(class G1_C8`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.A.B<(class G1_C8`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.A.B<(class G1_C8`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.B.T<T1,(class G1_C8`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.B.T<T1,(class G1_C8`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.B.A<(class G1_C8`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.B.A<(class G1_C8`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.B.B<(class G1_C8`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.B.B<(class G1_C8`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1403`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2222<object>()
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod4201()
		ldstr "G3_C1403::ClassMethod4201.15995()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod4200()
		ldstr "G3_C1403::ClassMethod4200.15994()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method5()
		ldstr "G3_C1403::Method5.15991()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method4()
		ldstr "G3_C1403::Method4.15989()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod2222<object>()
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass0>
		callvirt instance string class G3_C1403`1<class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1403`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2222<object>()
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod4201()
		ldstr "G3_C1403::ClassMethod4201.15995()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod4200()
		ldstr "G3_C1403::ClassMethod4200.15994()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method5()
		ldstr "G3_C1403::Method5.15991()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method4()
		ldstr "G3_C1403::Method4.15989()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod2222<object>()
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1403`1<class BaseClass1>
		callvirt instance string class G3_C1403`1<class BaseClass1>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C430`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2222<object>()
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C430`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2222<object>()
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C430`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2222<object>()
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C430`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2222<object>()
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2221<object>()
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2220()
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2219()
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1403`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.A<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method0.MI.8867()#G2_C430::Method1.MI.8869()#G2_C430::Method2.MI.8871<System.Object>()#G2_C430::Method3.8872<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1403`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1403`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.T.T<class BaseClass0,class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.T<class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.A<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G3_C1403::ClassMethod4200.15994()#G3_C1403::ClassMethod4201.15995()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G3_C1403::Method4.15989()#G3_C1403::Method5.15991()#G3_C1403::Method6.15993<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G3_C1403.T<class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G3_C1403::ClassMethod4200.15994()#G3_C1403::ClassMethod4201.15995()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G3_C1403::Method4.15989()#G3_C1403::Method5.15991()#G3_C1403::Method6.15993<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G3_C1403.A<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1403`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1403`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1403`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1403`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.A<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method0.MI.8867()#G2_C430::Method1.MI.8869()#G2_C430::Method2.MI.8871<System.Object>()#G2_C430::Method3.8872<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.T.T<class BaseClass0,class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.T<class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.B<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G3_C1403::ClassMethod4200.15994()#G3_C1403::ClassMethod4201.15995()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G3_C1403::Method4.15989()#G3_C1403::Method5.15991()#G3_C1403::Method6.15993<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G3_C1403.T<class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G3_C1403::ClassMethod2222.MI.15996<System.Object>()#G3_C1403::ClassMethod4200.15994()#G3_C1403::ClassMethod4201.15995()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G3_C1403::Method4.15989()#G3_C1403::Method5.15991()#G3_C1403::Method6.15993<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G3_C1403.B<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1403`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1403`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1403::Method4.MI.15990()#G3_C1403::Method5.MI.15992()#G3_C1403::Method6.15993<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1403`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C430`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.A<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method0.MI.8867()#G2_C430::Method1.MI.8869()#G2_C430::Method2.MI.8871<System.Object>()#G2_C430::Method3.8872<System.Object>()#"
		call void Framework::M.IBase0<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.T.T<class BaseClass0,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.A<class G2_C430`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C430`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.A<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method0.MI.8867()#G2_C430::Method1.MI.8869()#G2_C430::Method2.MI.8871<System.Object>()#G2_C430::Method3.8872<System.Object>()#"
		call void Framework::M.IBase0<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.T.T<class BaseClass0,class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.T<class BaseClass1,class G2_C430`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.A.B<class G2_C430`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C430`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.B<class G2_C430`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C430`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method0.MI.8867()#G2_C430::Method1.MI.8869()#G2_C430::Method2.MI.8871<System.Object>()#G2_C430::Method3.8872<System.Object>()#"
		call void Framework::M.IBase0<class G2_C430`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C430`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.T.T<class BaseClass1,class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.B.T<class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.B.A<class G2_C430`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C430`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C430`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.B<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method0.MI.8867()#G2_C430::Method1.MI.8869()#G2_C430::Method2.MI.8871<System.Object>()#G2_C430::Method3.8872<System.Object>()#"
		call void Framework::M.IBase0<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.T.T<class BaseClass1,class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.B.T<class BaseClass1,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::ClassMethod1332.MI.8878()#G2_C430::ClassMethod1333.MI.8879()#G2_C430::ClassMethod1334.MI.8880<System.Object>()#G2_C430::ClassMethod1335.MI.8881<System.Object>()#G2_C430::ClassMethod2219.8874()#G2_C430::ClassMethod2220.8875()#G2_C430::ClassMethod2221.8876<System.Object>()#G2_C430::ClassMethod2222.8877<System.Object>()#G2_C430::Method0.8866()#G2_C430::Method1.8868()#G2_C430::Method2.8870<System.Object>()#G2_C430::Method3.8872<System.Object>()#G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.G2_C430.B.B<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C430`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C430::Method7.8873<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C430`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.B<class G1_C8`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C8`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G1_C8`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.A<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass1,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.B.B<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1403`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2222<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2221<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2220()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2219()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod4201()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::ClassMethod4201.15995()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod4200()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::ClassMethod4200.15994()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method5()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method5.15991()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method4()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method4.15989()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod2222<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod2221<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod2220()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod2219()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method1()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::Method0()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G3_C1403`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1403`1<class BaseClass0>)
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1403`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2222<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2221<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2220()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2219()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod4201()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::ClassMethod4201.15995()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod4200()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::ClassMethod4200.15994()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method5()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method5.15991()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method4()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method4.15989()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod2222<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::ClassMethod2222.MI.15996<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod2221<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod2220()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod2219()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method1()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::Method0()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod1333()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1403`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1403`1<class BaseClass1>::ClassMethod1332()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G3_C1403`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method4.MI.15990()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method5.MI.15992()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1403`1<class BaseClass1>)
		ldstr "G3_C1403::Method6.15993<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1403`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C430`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2222<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2221<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2220()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod2219()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C430`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2222<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2221<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2220()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod2219()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C430`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2222<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2221<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2220()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod2219()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C430`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method0.MI.8867()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method1.MI.8869()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method2.MI.8871<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "IBase0 on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2222<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2222.8877<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2221<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2221.8876<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2220()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2220.8875()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod2219()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod2219.8874()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method3.8872<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method2.8870<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method1.8868()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method0.8866()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1335.MI.8881<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1334.MI.8880<System.Object>()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1333.MI.8879()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C430`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C430`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::ClassMethod1332.MI.8878()"
		ldstr "class G2_C430`2<class BaseClass1,class BaseClass1> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C430`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C430::Method7.8873<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C430`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G1_C8`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C8`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C8`2<class BaseClass1,class BaseClass1>"
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
