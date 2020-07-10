// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated900 { .hash algorithm 0x00008004 }
.module Generated900.exe

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
.class public G3_C1372`1<T0> 
		extends class G2_C399`2<class BaseClass1,!T0>
		implements class IBase2`2<class BaseClass0,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1372::Method7.15819<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C399<class BaseClass1,T0>.ClassMethod2147'() cil managed noinlining { 
		.override method instance string class G2_C399`2<class BaseClass1,!T0>::ClassMethod2147()
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C399<class BaseClass1,T0>.ClassMethod2148'() cil managed noinlining { 
		.override method instance string class G2_C399`2<class BaseClass1,!T0>::ClassMethod2148()
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C399<class BaseClass1,T0>.ClassMethod2149'<M0>() cil managed noinlining { 
		.override method instance string class G2_C399`2<class BaseClass1,!T0>::ClassMethod2149<[1]>()
		ldstr "G3_C1372::ClassMethod2149.MI.15822<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C399`2<class BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public G2_C399`2<T0, T1> 
		extends class G1_C8`2<class BaseClass0,class BaseClass0>
		implements class IBase2`2<class BaseClass0,class BaseClass1>, class IBase1`1<!T1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C399::Method7.8541<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass0,class BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<[1]>()
		ldstr "G2_C399::Method7.MI.8542<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C399::Method4.8543()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C399::Method5.8544()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T1>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C399::Method6.8546<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T1>::Method6<[1]>()
		ldstr "G2_C399::Method6.MI.8547<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2147() cil managed noinlining { 
		ldstr "G2_C399::ClassMethod2147.8548()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2148() cil managed noinlining { 
		ldstr "G2_C399::ClassMethod2148.8549()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2149<M0>() cil managed noinlining { 
		ldstr "G2_C399::ClassMethod2149.8550<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass0,class BaseClass0>.ClassMethod1332'() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass0,class BaseClass0>.ClassMethod1333'() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass0,class BaseClass0>.ClassMethod1334'<M0>() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<[1]>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class BaseClass0,class BaseClass0>.ClassMethod1335'<M0>() cil managed noinlining { 
		.override method instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<[1]>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C8`2<class BaseClass0,class BaseClass0>::.ctor()
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
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
	.method static void M.G3_C1372.T<T0,(class G3_C1372`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1372.T<T0,(class G3_C1372`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1372.A<(class G3_C1372`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1372.A<(class G3_C1372`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1372.B<(class G3_C1372`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1372.B<(class G3_C1372`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1372`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.T.T<T0,T1,(class G2_C399`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.T.T<T0,T1,(class G2_C399`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.A.T<T1,(class G2_C399`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.A.T<T1,(class G2_C399`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.A.A<(class G2_C399`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.A.A<(class G2_C399`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.A.B<(class G2_C399`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.A.B<(class G2_C399`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.B.T<T1,(class G2_C399`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.B.T<T1,(class G2_C399`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.B.A<(class G2_C399`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.B.A<(class G2_C399`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C399.B.B<(class G2_C399`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C399.B.B<(class G2_C399`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2147()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2148()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2149<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1372`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2149<object>()
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2148()
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2147()
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod2149<object>()
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod2148()
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod2147()
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass0>
		callvirt instance string class G3_C1372`1<class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1372`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2149<object>()
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2148()
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2147()
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod2149<object>()
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod2148()
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod2147()
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1372`1<class BaseClass1>
		callvirt instance string class G3_C1372`1<class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C399`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2149<object>()
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2148()
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2147()
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C399`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2149<object>()
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2148()
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2147()
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C399`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2149<object>()
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2148()
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2147()
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C399`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2149<object>()
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2148()
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2147()
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1372`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G3_C1372`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1372`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1372`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1372`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1372`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1372`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.T.T<class BaseClass1,class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.T<class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.A<class G3_C1372`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1372`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.G3_C1372.T<class BaseClass0,class G3_C1372`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.G3_C1372.A<class G3_C1372`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1372`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G3_C1372`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1372`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1372`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.T.T<class BaseClass1,class BaseClass1,class G3_C1372`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.T<class BaseClass1,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.B<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.B<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.A<class G3_C1372`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.G3_C1372.T<class BaseClass1,class G3_C1372`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G3_C1372::ClassMethod2147.MI.15820()#G3_C1372::ClassMethod2148.MI.15821()#G3_C1372::ClassMethod2149.MI.15822<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G3_C1372::Method7.15819<System.Object>()#"
		call void Framework::M.G3_C1372.B<class G3_C1372`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C399`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G2_C399`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C399`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G2_C399`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C399`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.A.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.A.A<class G2_C399`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C399`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C399`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.T.T<class BaseClass0,class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.A.T<class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.A.B<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C399`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C399`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C399`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G2_C399`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C399`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G2_C399`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C399`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.T.T<class BaseClass1,class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.A<class G2_C399`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C399`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C399`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Framework::M.G1_C8.A.A<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Framework::M.IBase0<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method7.MI.8542<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.T.T<class BaseClass1,class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.T<class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::ClassMethod1332.MI.8551()#G2_C399::ClassMethod1333.MI.8552()#G2_C399::ClassMethod1334.MI.8553<System.Object>()#G2_C399::ClassMethod1335.MI.8554<System.Object>()#G2_C399::ClassMethod2147.8548()#G2_C399::ClassMethod2148.8549()#G2_C399::ClassMethod2149.8550<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C399::Method4.8543()#G2_C399::Method5.8544()#G2_C399::Method6.8546<System.Object>()#G2_C399::Method7.8541<System.Object>()#"
		call void Framework::M.G2_C399.B.B<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass1,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.B<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.T<class BaseClass0,class G2_C399`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C399::Method4.8543()#G2_C399::Method5.MI.8545()#G2_C399::Method6.MI.8547<System.Object>()#"
		call void Framework::M.IBase1.A<class G2_C399`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1372`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2149<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2148()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2147()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod2149<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod2148()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod2147()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method5()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method4()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method1()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass0>::Method0()
		calli default string(class G3_C1372`1<class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1372`1<class BaseClass0> on type class G3_C1372`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1372`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2149<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2148()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2147()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::Method7.15819<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod2149<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::ClassMethod2149.MI.15822<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod2148()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::ClassMethod2148.MI.15821()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod2147()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G3_C1372::ClassMethod2147.MI.15820()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method5()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method4()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod1333()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::ClassMethod1332()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method1()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1372`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1372`1<class BaseClass1>::Method0()
		calli default string(class G3_C1372`1<class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1372`1<class BaseClass1> on type class G3_C1372`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C399`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2149<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2148()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod2147()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C399`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2149<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2148()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod2147()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C399`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2149<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2148()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod2147()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C399`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C8`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C8`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method7.MI.8542<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2149<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod2149.8550<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2148()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod2148.8549()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod2147()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod2147.8548()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method6.8546<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method5.8544()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method7.8541<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1335<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1335.MI.8554<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1334<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1334.MI.8553<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1333()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1333.MI.8552()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::ClassMethod1332()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::ClassMethod1332.MI.8551()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C399`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C399`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C399`2<class BaseClass1,class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method4.8543()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method5.MI.8545()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C399`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C399::Method6.MI.8547<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C399`2<class BaseClass1,class BaseClass1>"
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
