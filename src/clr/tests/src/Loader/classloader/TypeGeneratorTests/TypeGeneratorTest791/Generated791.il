// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated791 { .hash algorithm 0x00008004 }
.module Generated791.exe

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
.class public G3_C1263`1<T0> 
		extends class G2_C314`2<class BaseClass1,!T0>
		implements class IBase2`2<class BaseClass0,class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1263::Method7.15188<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3901() cil managed noinlining { 
		ldstr "G3_C1263::ClassMethod3901.15189()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3902<M0>() cil managed noinlining { 
		ldstr "G3_C1263::ClassMethod3902.15190<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C314<class BaseClass1,T0>.ClassMethod1959'() cil managed noinlining { 
		.override method instance string class G2_C314`2<class BaseClass1,!T0>::ClassMethod1959()
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C314<class BaseClass1,T0>.ClassMethod1961'<M0>() cil managed noinlining { 
		.override method instance string class G2_C314`2<class BaseClass1,!T0>::ClassMethod1961<[1]>()
		ldstr "G3_C1263::ClassMethod1961.MI.15192<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C314`2<class BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public G2_C314`2<T0, T1> 
		extends class G1_C6`2<!T1,class BaseClass0>
		implements IBase0, class IBase2`2<class BaseClass1,!T1> 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C314::Method0.7676()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C314::Method1.7678()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method1'() cil managed noinlining { 
		.override method instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C314::Method2.7680<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C314::Method2.MI.7681<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C314::Method3.7682<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G2_C314::Method3.MI.7683<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C314::Method7.7684<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<class BaseClass1,!T1>::Method7<[1]>()
		ldstr "G2_C314::Method7.MI.7685<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1959() cil managed noinlining { 
		ldstr "G2_C314::ClassMethod1959.7686()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1960<M0>() cil managed noinlining { 
		ldstr "G2_C314::ClassMethod1960.7687<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1961<M0>() cil managed noinlining { 
		ldstr "G2_C314::ClassMethod1961.7688<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T1,class BaseClass0>.ClassMethod1326'() cil managed noinlining { 
		.override method instance string class G1_C6`2<!T1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T1,class BaseClass0>.ClassMethod1327'<M0>() cil managed noinlining { 
		.override method instance string class G1_C6`2<!T1,class BaseClass0>::ClassMethod1327<[1]>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C6`2<!T1,class BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public G1_C6`2<T0, T1> 
		implements class IBase2`2<class BaseClass1,!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C6::Method7.4808<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1326() cil managed noinlining { 
		ldstr "G1_C6::ClassMethod1326.4809()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1327<M0>() cil managed noinlining { 
		ldstr "G1_C6::ClassMethod1327.4810<"
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
	.method static void M.G3_C1263.T<T0,(class G3_C1263`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1263.T<T0,(class G3_C1263`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod3901()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::ClassMethod3902<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1263.A<(class G3_C1263`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1263.A<(class G3_C1263`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod3901()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::ClassMethod3902<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1263.B<(class G3_C1263`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1263.B<(class G3_C1263`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod3901()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::ClassMethod3902<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1263`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.T.T<T0,T1,(class G2_C314`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.T.T<T0,T1,(class G2_C314`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.A.T<T1,(class G2_C314`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.A.T<T1,(class G2_C314`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.A.A<(class G2_C314`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.A.A<(class G2_C314`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.A.B<(class G2_C314`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.A.B<(class G2_C314`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.B.T<T1,(class G2_C314`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.B.T<T1,(class G2_C314`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.B.A<(class G2_C314`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.B.A<(class G2_C314`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C314.B.B<(class G2_C314`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C314.B.B<(class G2_C314`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1959()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1960<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1961<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method static void M.G1_C6.T.T<T0,T1,(class G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class G1_C6`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class G1_C6`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class G1_C6`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class G1_C6`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class G1_C6`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class G1_C6`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class G1_C6`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class G1_C6`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class G1_C6`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class G1_C6`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class G1_C6`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class G1_C6`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1263`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1961<object>()
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1959()
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod3902<object>()
		ldstr "G3_C1263::ClassMethod3902.15190<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod3901()
		ldstr "G3_C1263::ClassMethod3901.15189()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod1961<object>()
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod1959()
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass0>
		callvirt instance string class G3_C1263`1<class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1263`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1961<object>()
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1959()
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod3902<object>()
		ldstr "G3_C1263::ClassMethod3902.15190<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod3901()
		ldstr "G3_C1263::ClassMethod3901.15189()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod1961<object>()
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod1959()
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1263`1<class BaseClass1>
		callvirt instance string class G3_C1263`1<class BaseClass1>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C314`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1961<object>()
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1959()
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C314`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1961<object>()
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1959()
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C314`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1961<object>()
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1959()
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C314`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1961<object>()
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1960<object>()
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1959()
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method3<object>()
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method2<object>()
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method1()
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method0()
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1263`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.A<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1263`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1263`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G2_C314.T.T<class BaseClass1,class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G2_C314.B.T<class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G2_C314.B.A<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method0.MI.7677()#G2_C314::Method1.MI.7679()#G2_C314::Method2.MI.7681<System.Object>()#G2_C314::Method3.MI.7683<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1263`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G3_C1263::ClassMethod3901.15189()#G3_C1263::ClassMethod3902.15190<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G3_C1263.T<class BaseClass0,class G3_C1263`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G3_C1263::ClassMethod3901.15189()#G3_C1263::ClassMethod3902.15190<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G3_C1263.A<class G3_C1263`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1263`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G3_C1263`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass0,class G3_C1263`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.A<class G3_C1263`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1263`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1263`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G2_C314.T.T<class BaseClass1,class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G2_C314.B.T<class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G2_C314.B.B<class G3_C1263`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method0.MI.7677()#G2_C314::Method1.MI.7679()#G2_C314::Method2.MI.7681<System.Object>()#G2_C314::Method3.MI.7683<System.Object>()#"
		call void Framework::M.IBase0<class G3_C1263`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G3_C1263::ClassMethod3901.15189()#G3_C1263::ClassMethod3902.15190<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G3_C1263.T<class BaseClass1,class G3_C1263`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G3_C1263::ClassMethod1959.MI.15191()#G2_C314::ClassMethod1960.7687<System.Object>()#G3_C1263::ClassMethod1961.MI.15192<System.Object>()#G3_C1263::ClassMethod3901.15189()#G3_C1263::ClassMethod3902.15190<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G3_C1263::Method7.15188<System.Object>()#"
		call void Framework::M.G3_C1263.B<class G3_C1263`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C314`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.A<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.T.T<class BaseClass0,class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.A.T<class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.A.A<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method0.MI.7677()#G2_C314::Method1.MI.7679()#G2_C314::Method2.MI.7681<System.Object>()#G2_C314::Method3.MI.7683<System.Object>()#"
		call void Framework::M.IBase0<class G2_C314`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C314`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass0,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.A<class G2_C314`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C314`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C314`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.T.T<class BaseClass0,class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.A.T<class BaseClass1,class G2_C314`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.A.B<class G2_C314`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method0.MI.7677()#G2_C314::Method1.MI.7679()#G2_C314::Method2.MI.7681<System.Object>()#G2_C314::Method3.MI.7683<System.Object>()#"
		call void Framework::M.IBase0<class G2_C314`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C314`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.A<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.T.T<class BaseClass1,class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.B.T<class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.B.A<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method0.MI.7677()#G2_C314::Method1.MI.7679()#G2_C314::Method2.MI.7681<System.Object>()#G2_C314::Method3.MI.7683<System.Object>()#"
		call void Framework::M.IBase0<class G2_C314`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C314`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass0,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.A<class G2_C314`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C314`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::Method7.MI.7685<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C314`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.T.T<class BaseClass1,class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.B.T<class BaseClass1,class G2_C314`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C314::ClassMethod1326.MI.7689()#G2_C314::ClassMethod1327.MI.7690<System.Object>()#G2_C314::ClassMethod1959.7686()#G2_C314::ClassMethod1960.7687<System.Object>()#G2_C314::ClassMethod1961.7688<System.Object>()#G2_C314::Method0.7676()#G2_C314::Method1.7678()#G2_C314::Method2.7680<System.Object>()#G2_C314::Method3.7682<System.Object>()#G2_C314::Method7.7684<System.Object>()#"
		call void Framework::M.G2_C314.B.B<class G2_C314`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C314::Method0.MI.7677()#G2_C314::Method1.MI.7679()#G2_C314::Method2.MI.7681<System.Object>()#G2_C314::Method3.MI.7683<System.Object>()#"
		call void Framework::M.IBase0<class G2_C314`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.A<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.G1_C6.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1263`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1961<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1960<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1959()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod3902<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::ClassMethod3902.15190<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod3901()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::ClassMethod3901.15189()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod1961<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod1960<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod1959()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::Method1()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::Method0()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1263`1<class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G3_C1263`1<class BaseClass0> on type class G3_C1263`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1263`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1961<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1960<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1959()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod3902<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::ClassMethod3902.15190<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod3901()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::ClassMethod3901.15189()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::Method7.15188<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod1961<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::ClassMethod1961.MI.15192<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod1960<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod1959()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G3_C1263::ClassMethod1959.MI.15191()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::Method1()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::Method0()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1263`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1263`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1263`1<class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G3_C1263`1<class BaseClass1> on type class G3_C1263`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C314`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1961<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1960<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1959()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method1()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::Method0()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C314`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1961<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1960<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1959()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method1()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::Method0()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C314`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1961<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1960<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1959()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method1()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::Method0()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C314`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method7.MI.7685<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1961<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1961.7688<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1960<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1960.7687<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1959()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1959.7686()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method7.7684<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method3.7682<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method2.7680<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method1()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method1.7678()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::Method0()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method0.7676()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1327.MI.7690<System.Object>()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C314`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C314`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::ClassMethod1326.MI.7689()"
		ldstr "class G2_C314`2<class BaseClass1,class BaseClass1> on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method0.MI.7677()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method1.MI.7679()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method2.MI.7681<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C314`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C314::Method3.MI.7683<System.Object>()"
		ldstr "IBase0 on type class G2_C314`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G1_C6`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G1_C6`2<class BaseClass1,class BaseClass1>"
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
