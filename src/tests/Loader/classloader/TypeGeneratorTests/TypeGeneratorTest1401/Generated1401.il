// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1401.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1401.BaseClass1 
		extends Generated1401.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1401.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1401.G3_C1873`1<T0> 
		extends class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		implements Generated1401.IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1873::Method0.18462()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1873::Method1.18463()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1873::Method2.18464<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1873::Method3.18465<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5137() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5138() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5139<M0>() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5139.18468<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5140<M0>() cil managed noinlining { 
		ldstr "G3_C1873::ClassMethod5140.18469<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated1401.G2_C809`2<T0, T1> 
		extends class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		implements class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C809::Method7.12470<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3000<M0>() cil managed noinlining { 
		ldstr "G2_C809::ClassMethod3000.12471<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1401.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public abstract Generated1401.G1_C15`2<T0, T1> 
		implements class Generated1401.IBase2`2<!T1,!T1>, class Generated1401.IBase1`1<class Generated1401.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method7.4885<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1401.IBase2`2<!T1,!T1>::Method7<[1]>()
		ldstr "G1_C15::Method7.MI.4886<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C15::Method4.4887()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1401.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1401.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C15::Method6.4891<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1401.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<[1]>()
		ldstr "G1_C15::Method6.MI.4892<"
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
.class interface public abstract Generated1401.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class interface public abstract Generated1401.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1401.Generated1401 {
	.method static void M.BaseClass0<(Generated1401.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1401.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1401.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1401.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1873.T<T0,(class Generated1401.G3_C1873`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1873.T<T0,(class Generated1401.G3_C1873`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::ClassMethod5137()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::ClassMethod5138()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::ClassMethod5139<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::ClassMethod5140<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1873.A<(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1873.A<(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5137()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5138()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5139<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5140<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1873.B<(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1873.B<(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5137()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5138()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5139<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5140<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.T.T<T0,T1,(class Generated1401.G2_C809`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.T.T<T0,T1,(class Generated1401.G2_C809`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<!!T0,!!T1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.A.T<T1,(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.A.T<T1,(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.A.A<(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.A.A<(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.A.B<(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.A.B<(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.B.T<T1,(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.B.T<T1,(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.B.A<(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.B.A<(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C809.B.B<(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 10
		.locals init (string[] actualResults)
		ldc.i4.s    5
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C809.B.B<(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 5
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::ClassMethod3000<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated1401.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated1401.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1401.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1401.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1401.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1401.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.T.T<T0,T1,(class Generated1401.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class Generated1401.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class Generated1401.G1_C15`2<class Generated1401.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class Generated1401.G1_C15`2<class Generated1401.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class Generated1401.G1_C15`2<class Generated1401.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class Generated1401.G1_C15`2<class Generated1401.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1401.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1401.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1401.IBase2`2<class Generated1401.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1401.IBase2`2<class Generated1401.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1401.IBase2`2<class Generated1401.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1401.IBase2`2<class Generated1401.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1401.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1401.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1401.IBase1`1<class Generated1401.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1401.IBase1`1<class Generated1401.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1401.IBase1`1<class Generated1401.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1401.IBase1`1<class Generated1401.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<class Generated1401.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<class Generated1401.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1401.IBase1`1<class Generated1401.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5140<object>()
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5139<object>()
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5138()
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5137()
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1401.IBase0::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1401.IBase0::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1401.IBase0::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1401.IBase0::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5140<object>()
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5139<object>()
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5138()
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5137()
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1401.IBase0::Method0()
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1401.IBase0::Method1()
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1401.IBase0::Method2<object>()
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1401.IBase0::Method3<object>()
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::ClassMethod3000<object>()
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		callvirt instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
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
		newobj instance void class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.T<class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.A<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.T<class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.A<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G3_C1873.T<class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G3_C1873.A<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase0<class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>>(!!0,string)
		newobj instance void class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.T<class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.A<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.T<class Generated1401.BaseClass0,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.A<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G3_C1873.T<class Generated1401.BaseClass1,class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G3_C1873::ClassMethod5137.18466()#G3_C1873::ClassMethod5138.18467()#G3_C1873::ClassMethod5139.18468<System.Object>()#G3_C1873::ClassMethod5140.18469<System.Object>()#G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G3_C1873.B<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1873::Method0.18462()#G3_C1873::Method1.18463()#G3_C1873::Method2.18464<System.Object>()#G3_C1873::Method3.18465<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase0<class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>>(!!0,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.T<class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.A<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.A.T<class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.A.A<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>>(!!0,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.T<class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.A<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>>(!!0,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.T<class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.A<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.T<class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.A<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>>(!!0,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G1_C15.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.B.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.T<class Generated1401.BaseClass0,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase1.A<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.T.T<class Generated1401.BaseClass0,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.IBase2.A.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.T.T<class Generated1401.BaseClass1,class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.T<class Generated1401.BaseClass1,class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C809::Method7.12470<System.Object>()#"
		call void Generated1401.Generated1401::M.G2_C809.B.B<class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5140<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5139<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5138()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod5137()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method3<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method2<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method1()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method0()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::ClassMethod3000<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method0()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method1()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method2<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method3<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5140<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::ClassMethod5140.18469<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5139<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::ClassMethod5139.18468<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5138()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::ClassMethod5138.18467()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod5137()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::ClassMethod5137.18466()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method3<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method2<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method1()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method0()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::ClassMethod3000<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G3_C1873`1<class Generated1401.BaseClass1> on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method0()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method0.18462()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method1()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method1.18463()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method2<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method2.18464<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1401.IBase0::Method3<object>()
		calli default string(class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>)
		ldstr "G3_C1873::Method3.18465<System.Object>()"
		ldstr "IBase0 on type class Generated1401.G3_C1873`1<class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::ClassMethod3000<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::ClassMethod3000<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G1_C15`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase1`1<class Generated1401.BaseClass0>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1401.BaseClass0> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1401.IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class IBase2`2<class Generated1401.BaseClass0,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::ClassMethod3000<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G2_C809::ClassMethod3000.12471<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method7<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G2_C809::Method7.12470<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method6<object>()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method5()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>::Method4()
		calli default string(class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1> on type class Generated1401.G2_C809`2<class Generated1401.BaseClass1,class Generated1401.BaseClass1>"
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
		call void Generated1401.Generated1401::MethodCallingTest()
		call void Generated1401.Generated1401::ConstrainedCallsTest()
		call void Generated1401.Generated1401::StructConstrainedInterfaceCallsTest()
		call void Generated1401.Generated1401::CalliTest()
		ldc.i4 100
		ret
	}
}
