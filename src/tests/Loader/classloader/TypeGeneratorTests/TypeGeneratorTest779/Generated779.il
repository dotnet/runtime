// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly Generated779 { .hash algorithm 0x00008004 }
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
.class public G3_C1251`1<T0> 
		extends class G2_C299`2<class BaseClass0,!T0>
		implements class IBase2`2<class BaseClass0,!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1251::Method7.15119<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3879() cil managed noinlining { 
		ldstr "G3_C1251::ClassMethod3879.15120()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3880<M0>() cil managed noinlining { 
		ldstr "G3_C1251::ClassMethod3880.15121<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C299<class BaseClass0,T0>.ClassMethod1928'<M0>() cil managed noinlining { 
		.override method instance string class G2_C299`2<class BaseClass0,!T0>::ClassMethod1928<[1]>()
		ldstr "G3_C1251::ClassMethod1928.MI.15122<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G2_C299`2<class BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public G2_C299`2<T0, T1> 
		extends class G1_C6`2<!T0,class BaseClass0>
		implements class IBase2`2<!T1,!T0>, class IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C299::Method7.7519<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C299::Method4.7520()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C299::Method5.7521()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C299::Method6.7523<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C299::Method6.MI.7524<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1927() cil managed noinlining { 
		ldstr "G2_C299::ClassMethod1927.7525()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1928<M0>() cil managed noinlining { 
		ldstr "G2_C299::ClassMethod1928.7526<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C6<T0,class BaseClass0>.ClassMethod1326'() cil managed noinlining { 
		.override method instance string class G1_C6`2<!T0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class G1_C6`2<!T0,class BaseClass0>::.ctor()
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
.class interface public abstract IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated779 {
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
	.method static void M.G3_C1251.T<T0,(class G3_C1251`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1251.T<T0,(class G3_C1251`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::ClassMethod3879()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::ClassMethod3880<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1251.A<(class G3_C1251`1<class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1251.A<(class G3_C1251`1<class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::ClassMethod3879()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::ClassMethod3880<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1251.B<(class G3_C1251`1<class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1251.B<(class G3_C1251`1<class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::ClassMethod3879()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::ClassMethod3880<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G3_C1251`1<class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.T.T<T0,T1,(class G2_C299`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.T.T<T0,T1,(class G2_C299`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.A.T<T1,(class G2_C299`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.A.T<T1,(class G2_C299`2<class BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.A.A<(class G2_C299`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.A.A<(class G2_C299`2<class BaseClass0,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.A.B<(class G2_C299`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.A.B<(class G2_C299`2<class BaseClass0,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.B.T<T1,(class G2_C299`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.B.T<T1,(class G2_C299`2<class BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.B.A<(class G2_C299`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.B.A<(class G2_C299`2<class BaseClass1,class BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C299.B.B<(class G2_C299`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C299.B.B<(class G2_C299`2<class BaseClass1,class BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1927()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1928<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method7<object>()
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
		newobj instance void class G3_C1251`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1928<object>()
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::ClassMethod3880<object>()
		ldstr "G3_C1251::ClassMethod3880.15121<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::ClassMethod3879()
		ldstr "G3_C1251::ClassMethod3879.15120()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::ClassMethod1928<object>()
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass0>
		callvirt instance string class G3_C1251`1<class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G3_C1251`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1928<object>()
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::ClassMethod3880<object>()
		ldstr "G3_C1251::ClassMethod3880.15121<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::ClassMethod3879()
		ldstr "G3_C1251::ClassMethod3879.15120()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::Method7<object>()
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::ClassMethod1928<object>()
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G3_C1251`1<class BaseClass1>
		callvirt instance string class G3_C1251`1<class BaseClass1>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C299`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1928<object>()
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C299`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1928<object>()
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C299`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1928<object>()
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class G2_C299`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		callvirt instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1928<object>()
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1927()
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method6<object>()
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method5()
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method7<object>()
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		callvirt instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass1>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method4()
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method5()
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class IBase1`1<class BaseClass0>::Method6<object>()
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
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
		newobj instance void class G3_C1251`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G1_C6.A.T<class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G1_C6.A.A<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.A<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.A<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1251`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1251`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G2_C299.T.T<class BaseClass0,class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G2_C299.A.T<class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G2_C299.A.A<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.A<class G3_C1251`1<class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G3_C1251::ClassMethod3879.15120()#G3_C1251::ClassMethod3880.15121<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G3_C1251.T<class BaseClass0,class G3_C1251`1<class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G3_C1251::ClassMethod3879.15120()#G3_C1251::ClassMethod3880.15121<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G3_C1251.A<class G3_C1251`1<class BaseClass0>>(!!0,string)
		newobj instance void class G3_C1251`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G1_C6.A.T<class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G1_C6.A.A<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.A<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.A<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G2_C299.T.T<class BaseClass0,class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G2_C299.A.T<class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G2_C299.A.B<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass0,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.A<class G3_C1251`1<class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G3_C1251::ClassMethod3879.15120()#G3_C1251::ClassMethod3880.15121<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G3_C1251.T<class BaseClass1,class G3_C1251`1<class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G3_C1251::ClassMethod1928.MI.15122<System.Object>()#G3_C1251::ClassMethod3879.15120()#G3_C1251::ClassMethod3880.15121<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G3_C1251::Method7.15119<System.Object>()#"
		call void Generated779::M.G3_C1251.B<class G3_C1251`1<class BaseClass1>>(!!0,string)
		newobj instance void class G2_C299`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.A.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.A.A<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.A<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.A<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.T.T<class BaseClass0,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.A.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.A.A<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.A<class G2_C299`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C299`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.A.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.A.A<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.A<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.A<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.T.T<class BaseClass0,class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.A.T<class BaseClass1,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.A.B<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass0,class G2_C299`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.A<class G2_C299`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G2_C299`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.B.T<class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.B.A<class G2_C299`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G2_C299`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G2_C299`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.T.T<class BaseClass1,class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.B.T<class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.B.A<class G2_C299`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.B<class G2_C299`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.A<class G2_C299`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G2_C299`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.B.T<class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G1_C6.B.A<class G2_C299`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G2_C299`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G2_C299`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.T.T<class BaseClass1,class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.B.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::ClassMethod1326.MI.7527()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C299::ClassMethod1927.7525()#G2_C299::ClassMethod1928.7526<System.Object>()#G2_C299::Method4.7520()#G2_C299::Method5.7521()#G2_C299::Method6.7523<System.Object>()#G2_C299::Method7.7519<System.Object>()#"
		call void Generated779::M.G2_C299.B.B<class G2_C299`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass1,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.B<class G2_C299`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.T<class BaseClass0,class G2_C299`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C299::Method4.7520()#G2_C299::Method5.MI.7522()#G2_C299::Method6.MI.7524<System.Object>()#"
		call void Generated779::M.IBase1.A<class G2_C299`2<class BaseClass1,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass0,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.A<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass0,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G1_C6`2<class BaseClass0,class BaseClass1>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass1,class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.B.T<class BaseClass0,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.B.A<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass0>>(!!0,string)
		newobj instance void class G1_C6`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.G1_C6.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass1,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.B.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.T.T<class BaseClass0,class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.T<class BaseClass1,class G1_C6`2<class BaseClass1,class BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated779::M.IBase2.A.B<class G1_C6`2<class BaseClass1,class BaseClass1>>(!!0,string)
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
		newobj instance void class G3_C1251`1<class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1928<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1927()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::ClassMethod3880<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::ClassMethod3880.15121<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::ClassMethod3879()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::ClassMethod3879.15120()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::ClassMethod1928<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::ClassMethod1927()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::Method5()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::Method4()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1251`1<class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G3_C1251`1<class BaseClass0> on type class G3_C1251`1<class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G3_C1251`1<class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1928<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1927()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::ClassMethod3880<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::ClassMethod3880.15121<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::ClassMethod3879()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::ClassMethod3879.15120()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::Method7<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::Method7.15119<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::ClassMethod1928<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G3_C1251::ClassMethod1928.MI.15122<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::ClassMethod1927()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::Method6<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::Method5()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::Method4()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G3_C1251`1<class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G3_C1251`1<class BaseClass1>::ClassMethod1326()
		calli default string(class G3_C1251`1<class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G3_C1251`1<class BaseClass1> on type class G3_C1251`1<class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C299`2<class BaseClass0,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1928<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1927()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method5()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method4()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass0>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C299`2<class BaseClass0,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass0,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1928<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1927()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method5()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method4()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass0,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass0,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass0,class BaseClass1>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass0,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C299`2<class BaseClass1,class BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1928<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1927()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method5()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method4()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass0>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class G2_C299`2<class BaseClass1,class BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G1_C6`2<class BaseClass1,class BaseClass0>
		ldloc.0
		ldvirtftn instance string class G1_C6`2<class BaseClass1,class BaseClass0>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G1_C6`2<class BaseClass1,class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase2`2<class BaseClass0,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class IBase2`2<class BaseClass0,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1928<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1928.7526<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1927()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1927.7525()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method6.7523<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method5()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method5.7521()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method4()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::Method7<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method7.7519<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1327<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class G2_C299`2<class BaseClass1,class BaseClass1>
		ldloc.0
		ldvirtftn instance string class G2_C299`2<class BaseClass1,class BaseClass1>::ClassMethod1326()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::ClassMethod1326.MI.7527()"
		ldstr "class G2_C299`2<class BaseClass1,class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method4()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method5()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass1>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass1> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method4()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method4.7520()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method5()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method5.MI.7522()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class IBase1`1<class BaseClass0>::Method6<object>()
		calli default string(class G2_C299`2<class BaseClass1,class BaseClass1>)
		ldstr "G2_C299::Method6.MI.7524<System.Object>()"
		ldstr "class IBase1`1<class BaseClass0> on type class G2_C299`2<class BaseClass1,class BaseClass1>"
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
		call void Generated779::MethodCallingTest()
		call void Generated779::ConstrainedCallsTest()
		call void Generated779::StructConstrainedInterfaceCallsTest()
		call void Generated779::CalliTest()
		ldc.i4 100
		ret
	}
}
