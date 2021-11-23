// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated621 { .hash algorithm 0x00008004 }
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
.class public G3_C1093`1<T0> 
		extends class G2_C71`1<class BaseClass0>
		implements class IBase2`2<!T0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1093::Method7.14260<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class IBase2`2<!T0,!T0>::Method7<[1]>()
		ldstr "G3_C1093::Method7.MI.14261<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3577() cil managed noinlining { 
		ldstr "G3_C1093::ClassMethod3577.14262()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3578() cil managed noinlining { 
		ldstr "G3_C1093::ClassMethod3578.14263()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3579<M0>() cil managed noinlining { 
		ldstr "G3_C1093::ClassMethod3579.14264<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3580<M0>() cil managed noinlining { 
		ldstr "G3_C1093::ClassMethod3580.14265<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C71<class BaseClass0>.ClassMethod1458'() cil managed noinlining { 
		.override method instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C71`1<class BaseClass0>::.ctor()
		ret
	}
}
.class public G2_C71`1<T0> 
		extends class G1_C6`2<class BaseClass1,class BaseClass1>
		implements IBase0, class IBase2`2<!T0,class BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C71::Method0.5391()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string IBase0::Method0()
		ldstr "G2_C71::Method0.MI.5392()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C71::Method1.5393()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C71::Method2.5394<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method2<[1]>()
		ldstr "G2_C71::Method2.MI.5395<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C71::Method3.5396<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string IBase0::Method3<[1]>()
		ldstr "G2_C71::Method3.MI.5397<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C71::Method7.5398<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1458() cil managed noinlining { 
		ldstr "G2_C71::ClassMethod1458.5399()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1459<M0>() cil managed noinlining { 
		ldstr "G2_C71::ClassMethod1459.5400<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
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
.class public auto ansi beforefieldinit Generated621 {
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
	.method static void M.G3_C1093.T<T0,(class G3_C1093`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1093.T<T0,(class G3_C1093`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod1458()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod1459<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod3577()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod3578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod3579<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::ClassMethod3580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1093.A<(class G3_C1093`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1093.A<(class G3_C1093`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod1458()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod1459<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod3577()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod3578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod3579<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::ClassMethod3580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1093.B<(class G3_C1093`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1093.B<(class G3_C1093`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod1458()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod1459<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod3577()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod3578()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod3579<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::ClassMethod3580<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1093`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C71.T<T0,(class G2_C71`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C71.T<T0,(class G2_C71`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::ClassMethod1458()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::ClassMethod1459<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C71.A<(class G2_C71`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C71.A<(class G2_C71`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C71.B<(class G2_C71`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C71.B<(class G2_C71`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::ClassMethod1458()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::ClassMethod1459<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C71`1<class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1093`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method0()
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod3580<object>()
		ldstr "G3_C1093::ClassMethod3580.14265<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod3579<object>()
		ldstr "G3_C1093::ClassMethod3579.14264<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod3578()
		ldstr "G3_C1093::ClassMethod3578.14263()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod3577()
		ldstr "G3_C1093::ClassMethod3577.14262()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod1459<object>()
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod1458()
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::Method0()
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass0>
		callvirt instance string class G3_C1093`1<class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1093`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method0()
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod3580<object>()
		ldstr "G3_C1093::ClassMethod3580.14265<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod3579<object>()
		ldstr "G3_C1093::ClassMethod3579.14264<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod3578()
		ldstr "G3_C1093::ClassMethod3578.14263()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod3577()
		ldstr "G3_C1093::ClassMethod3577.14262()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod1459<object>()
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod1458()
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::Method0()
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1093`1<class BaseClass1>
		callvirt instance string class G3_C1093`1<class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C71`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		ldstr "G2_C71::ClassMethod1458.5399()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::Method0()
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass0>
		callvirt instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C71`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::ClassMethod1459<object>()
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::ClassMethod1458()
		ldstr "G2_C71::ClassMethod1458.5399()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::Method3<object>()
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::Method2<object>()
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::Method0()
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C71`1<class BaseClass1>
		callvirt instance string class G2_C71`1<class BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string IBase0::Method0()
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method1()
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method2<object>()
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string IBase0::Method3<object>()
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C71`1<class BaseClass1>"
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
		newobj instance void class G3_C1093`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G3_C1093`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.T<class BaseClass1,class G3_C1093`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.B<class G3_C1093`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1093`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G3_C1093`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G3_C1093`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1093`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G3_C1093`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G3_C1093`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G2_C71.T<class BaseClass0,class G3_C1093`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G2_C71.A<class G3_C1093`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method0.MI.5392()#G2_C71::Method1.5393()#G2_C71::Method2.MI.5395<System.Object>()#G2_C71::Method3.MI.5397<System.Object>()#"
		call void Generated621::M.IBase0<class G3_C1093`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1093`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass0,class G3_C1093`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.A.A<class G3_C1093`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G3_C1093::ClassMethod3577.14262()#G3_C1093::ClassMethod3578.14263()#G3_C1093::ClassMethod3579.14264<System.Object>()#G3_C1093::ClassMethod3580.14265<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G3_C1093.T<class BaseClass0,class G3_C1093`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G3_C1093::ClassMethod3577.14262()#G3_C1093::ClassMethod3578.14263()#G3_C1093::ClassMethod3579.14264<System.Object>()#G3_C1093::ClassMethod3580.14265<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G3_C1093.A<class G3_C1093`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1093`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.T<class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.B<class G3_C1093`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G3_C1093`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G3_C1093`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G2_C71.T<class BaseClass0,class G3_C1093`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G2_C71.A<class G3_C1093`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method0.MI.5392()#G2_C71::Method1.5393()#G2_C71::Method2.MI.5395<System.Object>()#G2_C71::Method3.MI.5397<System.Object>()#"
		call void Generated621::M.IBase0<class G3_C1093`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1093`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass0,class G3_C1093`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.IBase2.A.A<class G3_C1093`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G3_C1093::ClassMethod3577.14262()#G3_C1093::ClassMethod3578.14263()#G3_C1093::ClassMethod3579.14264<System.Object>()#G3_C1093::ClassMethod3580.14265<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G3_C1093.T<class BaseClass1,class G3_C1093`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1093::ClassMethod1458.MI.14266()#G2_C71::ClassMethod1459.5400<System.Object>()#G3_C1093::ClassMethod3577.14262()#G3_C1093::ClassMethod3578.14263()#G3_C1093::ClassMethod3579.14264<System.Object>()#G3_C1093::ClassMethod3580.14265<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G3_C1093::Method7.14260<System.Object>()#"
		call void Generated621::M.G3_C1093.B<class G3_C1093`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C71`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G2_C71`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.T<class BaseClass1,class G2_C71`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.B<class G2_C71`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C71`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G2_C71`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G2_C71`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C71`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G2_C71`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G2_C71`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C71::ClassMethod1458.5399()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.G2_C71.T<class BaseClass0,class G2_C71`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C71::ClassMethod1458.5399()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.G2_C71.A<class G2_C71`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method0.MI.5392()#G2_C71::Method1.5393()#G2_C71::Method2.MI.5395<System.Object>()#G2_C71::Method3.MI.5397<System.Object>()#"
		call void Generated621::M.IBase0<class G2_C71`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C71`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass0,class G2_C71`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.A<class G2_C71`1<class BaseClass0>>(!!0,string)
		newobj instance void class G2_C71`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G2_C71`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.T<class BaseClass1,class G2_C71`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.B<class G2_C71`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C71`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G2_C71`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G2_C71`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C71`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G2_C71`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G2_C71`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C71::ClassMethod1458.5399()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.G2_C71.T<class BaseClass1,class G2_C71`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C71::ClassMethod1458.5399()#G2_C71::ClassMethod1459.5400<System.Object>()#G2_C71::Method0.5391()#G2_C71::Method1.5393()#G2_C71::Method2.5394<System.Object>()#G2_C71::Method3.5396<System.Object>()#G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.G2_C71.B<class G2_C71`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method0.MI.5392()#G2_C71::Method1.5393()#G2_C71::Method2.MI.5395<System.Object>()#G2_C71::Method3.MI.5397<System.Object>()#"
		call void Generated621::M.IBase0<class G2_C71`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C71`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass0,class G2_C71`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.B.A<class G2_C71`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C71`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass0,class G2_C71`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C71::Method7.5398<System.Object>()#"
		call void Generated621::M.IBase2.A.A<class G2_C71`1<class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.T<class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.A<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.G1_C6.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated621::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1093`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method1()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method0()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod3580<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::ClassMethod3580.14265<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod3579<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::ClassMethod3579.14264<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod3578()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::ClassMethod3578.14263()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod3577()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::ClassMethod3577.14262()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod1459<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod1458()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::Method1()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::Method0()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1093`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1093`1<class BaseClass0> on type class G3_C1093`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1093`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::Method7.MI.14261<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method1()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method0()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod3580<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::ClassMethod3580.14265<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod3579<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::ClassMethod3579.14264<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod3578()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::ClassMethod3578.14263()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod3577()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::ClassMethod3577.14262()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::Method7.14260<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod1459<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod1458()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G3_C1093::ClassMethod1458.MI.14266()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::Method3<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::Method2<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::Method1()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::Method0()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1093`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1093`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1093`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1093`1<class BaseClass1> on type class G3_C1093`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C71`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1459<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1458()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::ClassMethod1458.5399()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method3<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method2<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method1()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::Method0()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass0>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C71`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C71`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::ClassMethod1459<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::ClassMethod1459.5400<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::ClassMethod1458()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::ClassMethod1458.5399()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::Method3<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method3.5396<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::Method2<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method2.5394<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::Method1()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::Method0()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method0.5391()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C71`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C71`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C71`1<class BaseClass1> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method0()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method0.MI.5392()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method1()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method1.5393()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method2<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method2.MI.5395<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string IBase0::Method3<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method3.MI.5397<System.Object>()"
		ldstr "IBase0 on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C71`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C71`1<class BaseClass1>)
		ldstr "G2_C71::Method7.5398<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C71`1<class BaseClass1>"
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
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  10
		call void Generated621::MethodCallingTest()
		call void Generated621::ConstrainedCallsTest()
		call void Generated621::StructConstrainedInterfaceCallsTest()
		call void Generated621::CalliTest()
		ldc.i4 100
		ret
	}
}
