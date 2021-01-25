// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated693 { .hash algorithm 0x00008004 }
.module Generated693.exe

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
.class public G3_C1165`1<T0> 
		extends class G2_C233`2<class BaseClass1,class BaseClass1>
		implements class IBase2`2<class BaseClass0,class BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1165::Method7.14679<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3726() cil managed noinlining { 
		ldstr "G3_C1165::ClassMethod3726.14680()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3727() cil managed noinlining { 
		ldstr "G3_C1165::ClassMethod3727.14681()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3728<M0>() cil managed noinlining { 
		ldstr "G3_C1165::ClassMethod3728.14682<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3729<M0>() cil managed noinlining { 
		ldstr "G3_C1165::ClassMethod3729.14683<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C233<class BaseClass1,class BaseClass1>.ClassMethod1804'() cil managed noinlining { 
		.override method instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C233<class BaseClass1,class BaseClass1>.ClassMethod1805'<M0>() cil managed noinlining { 
		.override method instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<[1]>()
		ldstr "G3_C1165::ClassMethod1805.MI.14685<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C233<class BaseClass1,class BaseClass1>.ClassMethod1806'<M0>() cil managed noinlining { 
		.override method instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<[1]>()
		ldstr "G3_C1165::ClassMethod1806.MI.14686<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C233`2<class BaseClass1,class BaseClass1>::.ctor()
		ret
	}
}
.class public G2_C233`2<T0, T1> 
		extends class G1_C3`1<!T1>
		implements class IBase2`2<!T0,class BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C233::Method7.6917<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1803() cil managed noinlining { 
		ldstr "G2_C233::ClassMethod1803.6918()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1804() cil managed noinlining { 
		ldstr "G2_C233::ClassMethod1804.6919()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1805<M0>() cil managed noinlining { 
		ldstr "G2_C233::ClassMethod1805.6920<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1806<M0>() cil managed noinlining { 
		ldstr "G2_C233::ClassMethod1806.6921<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1318'() cil managed noinlining { 
		.override method instance string class G1_C3`1<!T1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1319'<M0>() cil managed noinlining { 
		.override method instance string class G1_C3`1<!T1>::ClassMethod1319<[1]>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1320'<M0>() cil managed noinlining { 
		.override method instance string class G1_C3`1<!T1>::ClassMethod1320<[1]>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<"
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
	.method static void M.G3_C1165.T<T0,(class G3_C1165`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1165.T<T0,(class G3_C1165`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod3726()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod3727()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod3728<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::ClassMethod3729<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1165.A<(class G3_C1165`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1165.A<(class G3_C1165`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod3726()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod3727()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod3728<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::ClassMethod3729<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1165.B<(class G3_C1165`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1165.B<(class G3_C1165`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod3726()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod3727()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod3728<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::ClassMethod3729<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1165`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.T.T<T0,T1,(class G2_C233`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.T.T<T0,T1,(class G2_C233`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.A.T<T1,(class G2_C233`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.A.T<T1,(class G2_C233`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.A.A<(class G2_C233`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.A.A<(class G2_C233`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.A.B<(class G2_C233`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.A.B<(class G2_C233`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.B.T<T1,(class G2_C233`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.B.T<T1,(class G2_C233`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.B.A<(class G2_C233`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.B.A<(class G2_C233`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C233.B.B<(class G2_C233`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C233.B.B<(class G2_C233`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
	.method public hidebysig static void MethodCallingTest() cil managed
	{
		.maxstack  10
		.locals init (object V_0)
		ldstr "========================== Method Calling Test =========================="
		call void [mscorlib]System.Console::WriteLine(string)
		newobj instance void class G3_C1165`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod3729<object>()
		ldstr "G3_C1165::ClassMethod3729.14683<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod3728<object>()
		ldstr "G3_C1165::ClassMethod3728.14682<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod3727()
		ldstr "G3_C1165::ClassMethod3727.14681()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod3726()
		ldstr "G3_C1165::ClassMethod3726.14680()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1806<object>()
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1805<object>()
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1804()
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass0>
		callvirt instance string class G3_C1165`1<class BaseClass0>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1165`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod3729<object>()
		ldstr "G3_C1165::ClassMethod3729.14683<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod3728<object>()
		ldstr "G3_C1165::ClassMethod3728.14682<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod3727()
		ldstr "G3_C1165::ClassMethod3727.14681()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod3726()
		ldstr "G3_C1165::ClassMethod3726.14680()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1806<object>()
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1805<object>()
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1804()
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1165`1<class BaseClass1>
		callvirt instance string class G3_C1165`1<class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C233`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1806<object>()
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1805<object>()
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1804()
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C233`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1806<object>()
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1805<object>()
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1804()
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C233`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass0>
		callvirt instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1806<object>()
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1805<object>()
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1804()
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C233`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C3`1<class BaseClass1>
		callvirt instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1165`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G1_C3.B<class G3_C1165`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1165`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G2_C233.T.T<class BaseClass1,class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G2_C233.B.T<class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G2_C233.B.B<class G3_C1165`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1165`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1165`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::ClassMethod3726.14680()#G3_C1165::ClassMethod3727.14681()#G3_C1165::ClassMethod3728.14682<System.Object>()#G3_C1165::ClassMethod3729.14683<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G3_C1165.T<class BaseClass0,class G3_C1165`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::ClassMethod3726.14680()#G3_C1165::ClassMethod3727.14681()#G3_C1165::ClassMethod3728.14682<System.Object>()#G3_C1165::ClassMethod3729.14683<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G3_C1165.A<class G3_C1165`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1165`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1165`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1165`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1165`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G1_C3.B<class G3_C1165`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G3_C1165`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G2_C233.T.T<class BaseClass1,class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G2_C233.B.T<class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G2_C233.B.B<class G3_C1165`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G3_C1165`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::ClassMethod3726.14680()#G3_C1165::ClassMethod3727.14681()#G3_C1165::ClassMethod3728.14682<System.Object>()#G3_C1165::ClassMethod3729.14683<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G3_C1165.T<class BaseClass1,class G3_C1165`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G3_C1165::ClassMethod1804.MI.14684()#G3_C1165::ClassMethod1805.MI.14685<System.Object>()#G3_C1165::ClassMethod1806.MI.14686<System.Object>()#G3_C1165::ClassMethod3726.14680()#G3_C1165::ClassMethod3727.14681()#G3_C1165::ClassMethod3728.14682<System.Object>()#G3_C1165::ClassMethod3729.14683<System.Object>()#G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.G3_C1165.B<class G3_C1165`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1165`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass0,class G3_C1165`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1165::Method7.14679<System.Object>()#"
		call void Framework::M.IBase2.A.A<class G3_C1165`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C233`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass0,class G2_C233`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.A<class G2_C233`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C233`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.T.T<class BaseClass0,class BaseClass0,class G2_C233`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.A.T<class BaseClass0,class G2_C233`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.A.A<class G2_C233`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C233`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.B<class G2_C233`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C233`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.T.T<class BaseClass0,class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.A.T<class BaseClass1,class G2_C233`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.A.B<class G2_C233`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C233`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass0,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.A<class G2_C233`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C233`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.T.T<class BaseClass1,class BaseClass0,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.B.T<class BaseClass0,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.B.A<class G2_C233`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C233`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C233`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.T<class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G1_C3.B<class G2_C233`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.T<class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.A.B<class G2_C233`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.T.T<class BaseClass1,class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.B.T<class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::ClassMethod1318.MI.6922()#G2_C233::ClassMethod1319.MI.6923<System.Object>()#G2_C233::ClassMethod1320.MI.6924<System.Object>()#G2_C233::ClassMethod1803.6918()#G2_C233::ClassMethod1804.6919()#G2_C233::ClassMethod1805.6920<System.Object>()#G2_C233::ClassMethod1806.6921<System.Object>()#G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.G2_C233.B.B<class G2_C233`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.B.T<class BaseClass1,class G2_C233`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C233::Method7.6917<System.Object>()#"
		call void Framework::M.IBase2.B.B<class G2_C233`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1165`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod3729<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod3729.14683<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod3728<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod3728.14682<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod3727()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod3727.14681()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod3726()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod3726.14680()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1806<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1805<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1804()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1803()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G3_C1165`1<class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass0>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1165`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1165`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod3729<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod3729.14683<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod3728<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod3728.14682<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod3727()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod3727.14681()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod3726()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod3726.14680()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1806<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod1806.MI.14686<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1805<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod1805.MI.14685<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1804()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::ClassMethod1804.MI.14684()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1803()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1165`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1165`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G3_C1165`1<class BaseClass1> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1165`1<class BaseClass1>)
		ldstr "G3_C1165::Method7.14679<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1165`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C233`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1806<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1805<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1804()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1803()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass0>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass0> on type class G2_C233`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C233`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1806<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1805<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1804()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1803()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass0,class BaseClass1>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C233`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1806<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1805<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1804()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1803()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass0>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass0> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C233`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C3`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C3`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G1_C3`1<class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1806<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1806.6921<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1805<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1805.6920<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1804()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1804.6919()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1803()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1803.6918()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1320<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1320.MI.6924<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1319<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1319.MI.6923<System.Object>()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C233`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C233`2<class BaseClass1,class BaseClass1>::ClassMethod1318()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::ClassMethod1318.MI.6922()"
		ldstr "class G2_C233`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C233`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C233::Method7.6917<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C233`2<class BaseClass1,class BaseClass1>"
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
