// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated762.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated762.BaseClass1 
		extends Generated762.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated762.BaseClass0::.ctor()
		ret
	}
}
.class public Generated762.G3_C1234`1<T0> 
		extends class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		implements class Generated762.IBase1`1<class Generated762.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1234::Method4.15019()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated762.BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method4()
		ldstr "G3_C1234::Method4.MI.15020()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1234::Method5.15021()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated762.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method5()
		ldstr "G3_C1234::Method5.MI.15022()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1234::Method6.15023<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated762.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method6<[1]>()
		ldstr "G3_C1234::Method6.MI.15024<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3852() cil managed noinlining { 
		ldstr "G3_C1234::ClassMethod3852.15025()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated762.G2_C283`2<T0, T1> 
		extends class Generated762.G1_C6`2<class Generated762.BaseClass0,!T1>
		implements class Generated762.IBase2`2<class Generated762.BaseClass0,!T0>, class Generated762.IBase1`1<class Generated762.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C283::Method7.7384<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated762.BaseClass0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated762.IBase2`2<class Generated762.BaseClass0,!T0>::Method7<[1]>()
		ldstr "G2_C283::Method7.MI.7385<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C283::Method4.7386()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated762.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.MI.7387()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C283::Method5.7388()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated762.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.MI.7389()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C283::Method6.7390<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated762.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public Generated762.G1_C6`2<T0, T1> 
		implements class Generated762.IBase2`2<class Generated762.BaseClass1,!T0> 
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
.class interface public abstract Generated762.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated762.Generated762 {
	.method static void M.BaseClass0<(Generated762.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated762.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated762.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated762.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1234.T<T0,(class Generated762.G3_C1234`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1234.T<T0,(class Generated762.G3_C1234`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::ClassMethod3852()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1234.A<(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1234.A<(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod3852()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1234.B<(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1234.B<(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod3852()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.T.T<T0,T1,(class Generated762.G2_C283`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.T.T<T0,T1,(class Generated762.G2_C283`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.A.T<T1,(class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.A.T<T1,(class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.A.A<(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.A.A<(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.A.B<(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.A.B<(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.B.T<T1,(class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.B.T<T1,(class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.B.A<(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.B.A<(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C283.B.B<(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C283.B.B<(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated762.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated762.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated762.IBase1`1<class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated762.IBase1`1<class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated762.IBase1`1<class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated762.IBase1`1<class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.T.T<T0,T1,(class Generated762.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.T.T<T0,T1,(class Generated762.G1_C6`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<!!T0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<!!T0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.T<T1,(class Generated762.G1_C6`2<class Generated762.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.T<T1,(class Generated762.G1_C6`2<class Generated762.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.A<(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.A<(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.A.B<(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.A.B<(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.T<T1,(class Generated762.G1_C6`2<class Generated762.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.T<T1,(class Generated762.G1_C6`2<class Generated762.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,!!T1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,!!T1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.A<(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.A<(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C6.B.B<(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C6.B.B<(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1326()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1327<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated762.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated762.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated762.IBase2`2<class Generated762.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated762.IBase2`2<class Generated762.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<class Generated762.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated762.IBase2`2<class Generated762.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated762.IBase2`2<class Generated762.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<class Generated762.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated762.G3_C1234`1<class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod3852()
		ldstr "G3_C1234::ClassMethod3852.15025()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G3_C1234::Method6.15023<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method5()
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method4()
		ldstr "G3_C1234::Method4.15019()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method4()
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method5()
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method6<object>()
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G3_C1234`1<class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod3852()
		ldstr "G3_C1234::ClassMethod3852.15025()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method6<object>()
		ldstr "G3_C1234::Method6.15023<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method5()
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method4()
		ldstr "G3_C1234::Method4.15019()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		callvirt instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method4()
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method5()
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method6<object>()
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method5()
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method4()
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method5()
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method4()
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1327<object>()
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1326()
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		callvirt instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
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
		newobj instance void class Generated762.G3_C1234`1<class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.A<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G3_C1234::Method5.15021()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G3_C1234::Method5.15021()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G3_C1234::Method5.15021()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.A<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.A<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1234::ClassMethod3852.15025()#G3_C1234::Method4.15019()#G3_C1234::Method5.15021()#G3_C1234::Method6.15023<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G3_C1234.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1234::ClassMethod3852.15025()#G3_C1234::Method4.15019()#G3_C1234::Method5.15021()#G3_C1234::Method6.15023<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G3_C1234.A<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.B<class Generated762.G3_C1234`1<class Generated762.BaseClass0>>(!!0,string)
		newobj instance void class Generated762.G3_C1234`1<class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.A<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G3_C1234::Method5.15021()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G3_C1234::Method5.15021()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G3_C1234::Method5.15021()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.A<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass0,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.A<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1234::ClassMethod3852.15025()#G3_C1234::Method4.15019()#G3_C1234::Method5.15021()#G3_C1234::Method6.15023<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G3_C1234.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G3_C1234::ClassMethod3852.15025()#G3_C1234::Method4.15019()#G3_C1234::Method5.15021()#G3_C1234::Method6.15023<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G3_C1234.B<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass1,class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1234::Method4.MI.15020()#G3_C1234::Method5.MI.15022()#G3_C1234::Method6.MI.15024<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.B<class Generated762.G3_C1234`1<class Generated762.BaseClass1>>(!!0,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.A<class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method7.MI.7385<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.T<class Generated762.BaseClass1,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G2_C283::Method4.7386()#G2_C283::Method5.7388()#G2_C283::Method6.7390<System.Object>()#G2_C283::Method7.7384<System.Object>()#"
		call void Generated762.Generated762::M.G2_C283.B.B<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.T<class Generated762.BaseClass0,class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C283::Method4.MI.7387()#G2_C283::Method5.MI.7389()#G2_C283::Method6.7390<System.Object>()#"
		call void Generated762.Generated762::M.IBase1.A<class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.A<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>>(!!0,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.A.B<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.A<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.A<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>>(!!0,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass1,class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.B.T<class Generated762.BaseClass0,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.B.A<class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>>(!!0,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.B.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::ClassMethod1326.4809()#G1_C6::ClassMethod1327.4810<System.Object>()#G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.G1_C6.B.B<class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass1,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.B.B<class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.T.T<class Generated762.BaseClass0,class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.T<class Generated762.BaseClass1,class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C6::Method7.4808<System.Object>()#"
		call void Generated762.Generated762::M.IBase2.A.B<class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated762.G3_C1234`1<class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod3852()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::ClassMethod3852.15025()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method6.15023<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method4.15019()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass0>)
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G3_C1234`1<class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod3852()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::ClassMethod3852.15025()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method6.15023<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method5.15021()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method4.15019()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G3_C1234`1<class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G3_C1234`1<class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G3_C1234`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method4()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method4.MI.15020()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method5()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method5.MI.15022()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass1>::Method6<object>()
		calli default string(class Generated762.G3_C1234`1<class Generated762.BaseClass1>)
		ldstr "G3_C1234::Method6.MI.15024<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass1> on type class Generated762.G3_C1234`1<class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.MI.7385<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method5.7388()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method4.7386()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method7.7384<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method4()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method4.MI.7387()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method5()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method5.MI.7389()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase1`1<class Generated762.BaseClass0>::Method6<object>()
		calli default string(class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G2_C283::Method6.7390<System.Object>()"
		ldstr "class IBase1`1<class Generated762.BaseClass0> on type class Generated762.G2_C283`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass0,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1327<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::ClassMethod1326()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1327<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1327.4810<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::ClassMethod1326()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::ClassMethod1326.4809()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass1,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated762.IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1>::Method7<object>()
		calli default string(class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>)
		ldstr "G1_C6::Method7.4808<System.Object>()"
		ldstr "class IBase2`2<class Generated762.BaseClass0,class Generated762.BaseClass1> on type class Generated762.G1_C6`2<class Generated762.BaseClass1,class Generated762.BaseClass1>"
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
		call void Generated762.Generated762::MethodCallingTest()
		call void Generated762.Generated762::ConstrainedCallsTest()
		call void Generated762.Generated762::StructConstrainedInterfaceCallsTest()
		call void Generated762.Generated762::CalliTest()
		ldc.i4 100
		ret
	}
}
