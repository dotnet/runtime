// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated936 { .hash algorithm 0x00008004 }
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
.class public G3_C1408`1<T0> 
		extends class G2_C433`2<!T0,class BaseClass0>
		implements class IBase2`2<class BaseClass1,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1408::Method7.16022<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4213() cil managed noinlining { 
		ldstr "G3_C1408::ClassMethod4213.16023()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4214<M0>() cil managed noinlining { 
		ldstr "G3_C1408::ClassMethod4214.16024<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C433<T0,class BaseClass0>.ClassMethod2228'<M0>() cil managed noinlining { 
		.override method instance string class G2_C433`2<!T0,class BaseClass0>::ClassMethod2228<[1]>()
		ldstr "G3_C1408::ClassMethod2228.MI.16025<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C433`2<!T0,class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C433`2<T0, T1> 
		extends class G1_C8`2<class BaseClass1,!T1>
		implements class IBase2`2<class BaseClass0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C433::Method7.8902<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,class BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<[1]>()
		ldstr "G2_C433::Method7.MI.8903<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2228<M0>() cil managed noinlining { 
		ldstr "G2_C433::ClassMethod2228.8904<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2229<M0>() cil managed noinlining { 
		ldstr "G2_C433::ClassMethod2229.8905<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass1,T1>.ClassMethod1332'() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass1,!T1>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass1,T1>.ClassMethod1334'<M0>() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass1,!T1>::ClassMethod1334<[1]>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C8`2<class BaseClass1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
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
.class public auto ansi beforefieldinit Generated936 {
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
	.method static void M.G3_C1408.T<T0,(class G3_C1408`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1408.T<T0,(class G3_C1408`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod4213()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::ClassMethod4214<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1408.A<(class G3_C1408`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1408.A<(class G3_C1408`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod4213()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::ClassMethod4214<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1408.B<(class G3_C1408`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1408.B<(class G3_C1408`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod4213()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::ClassMethod4214<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1408`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.T.T<T0,T1,(class G2_C433`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.T.T<T0,T1,(class G2_C433`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.A.T<T1,(class G2_C433`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.A.T<T1,(class G2_C433`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.A.A<(class G2_C433`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.A.A<(class G2_C433`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.A.B<(class G2_C433`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.A.B<(class G2_C433`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.B.T<T1,(class G2_C433`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.B.T<T1,(class G2_C433`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.B.A<(class G2_C433`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.B.A<(class G2_C433`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C433.B.B<(class G2_C433`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C433.B.B<(class G2_C433`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod2228<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod2229<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1408`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2228<object>()
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod4214<object>()
		ldstr "G3_C1408::ClassMethod4214.16024<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod4213()
		ldstr "G3_C1408::ClassMethod4213.16023()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod2228<object>()
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass0>
		callvirt instance string class G3_C1408`1<class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1408`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2228<object>()
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod4214<object>()
		ldstr "G3_C1408::ClassMethod4214.16024<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod4213()
		ldstr "G3_C1408::ClassMethod4213.16023()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod2228<object>()
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1408`1<class BaseClass1>
		callvirt instance string class G3_C1408`1<class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C433`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2228<object>()
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C433`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod2228<object>()
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C433`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2228<object>()
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C433`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod2229<object>()
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod2228<object>()
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1408`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.A<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.A<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1408`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1408`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.T.T<class BaseClass0,class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.A.T<class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.A.A<class G3_C1408`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G3_C1408::ClassMethod4213.16023()#G3_C1408::ClassMethod4214.16024<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.G3_C1408.T<class BaseClass0,class G3_C1408`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G3_C1408::ClassMethod4213.16023()#G3_C1408::ClassMethod4214.16024<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.G3_C1408.A<class G3_C1408`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1408`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.A<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.A<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1408`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1408`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.T.T<class BaseClass1,class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.B.T<class BaseClass0,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.B.A<class G3_C1408`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G3_C1408::ClassMethod4213.16023()#G3_C1408::ClassMethod4214.16024<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.G3_C1408.T<class BaseClass1,class G3_C1408`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G3_C1408::ClassMethod2228.MI.16025<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G3_C1408::ClassMethod4213.16023()#G3_C1408::ClassMethod4214.16024<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G3_C1408::Method7.16022<System.Object>()#"
		call void Generated936::M.G3_C1408.B<class G3_C1408`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C433`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.A<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.A<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.T.T<class BaseClass0,class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.A.T<class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.A.A<class G2_C433`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C433`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.B<class G2_C433`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G2_C433`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G2_C433`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G2_C433`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.T.T<class BaseClass0,class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.A.T<class BaseClass1,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.A.B<class G2_C433`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G2_C433`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G2_C433`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C433`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.A<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.A<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.T.T<class BaseClass1,class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.B.T<class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.B.A<class G2_C433`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C433`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.B<class G2_C433`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G2_C433`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G2_C433`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G2_C433`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.T.T<class BaseClass1,class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.B.T<class BaseClass1,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::ClassMethod1332.MI.8906()#G1_C8::ClassMethod1333.4830()#G2_C433::ClassMethod1334.MI.8907<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G2_C433::ClassMethod2228.8904<System.Object>()#G2_C433::ClassMethod2229.8905<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C433::Method7.8902<System.Object>()#"
		call void Generated936::M.G2_C433.B.B<class G2_C433`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G2_C433`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C433::Method7.MI.8903<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G2_C433`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.A.T<class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.A.A<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G1_C8`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.A.T<class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.A.B<class G1_C8`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G1_C8`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G1_C8`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.A<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.A<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass0,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.A<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G1_C8`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C8`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.T.T<class BaseClass1,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated936::M.G1_C8.B.B<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.B.B<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated936::M.IBase0<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.T<class BaseClass1,class G1_C8`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated936::M.IBase2.A.B<class G1_C8`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1408`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2229<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2228<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod4214<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::ClassMethod4214.16024<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod4213()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::ClassMethod4213.16023()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod2229<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod2228<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::Method1()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass0>::Method0()
		calli default string(class G3_C1408`1<class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1408`1<class BaseClass0> on type class G3_C1408`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1408`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2229<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2228<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod4214<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::ClassMethod4214.16024<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod4213()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::ClassMethod4213.16023()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::Method7.16022<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod2229<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod2228<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G3_C1408::ClassMethod2228.MI.16025<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod1333()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::ClassMethod1332()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::Method1()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1408`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1408`1<class BaseClass1>::Method0()
		calli default string(class G3_C1408`1<class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1408`1<class BaseClass1> on type class G3_C1408`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C433`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2229<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod2228<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C433`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod2229<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod2228<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C433`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2229<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod2228<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C433`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod2229<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::ClassMethod2229.8905<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod2228<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::ClassMethod2228.8904<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::Method7.8902<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1334.MI.8907<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::ClassMethod1332.MI.8906()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C433`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C433`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C433`2<class BaseClass1,class BaseClass1> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C433`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C433::Method7.MI.8903<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C433`2<class BaseClass1,class BaseClass1>"
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
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  10
		call void Generated936::MethodCallingTest()
		call void Generated936::ConstrainedCallsTest()
		call void Generated936::StructConstrainedInterfaceCallsTest()
		call void Generated936::CalliTest()
		ldc.i4 100
		ret
	}
}
