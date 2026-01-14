// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1479.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1479.BaseClass1 
		extends Generated1479.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1479.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1479.G3_C1951`1<T0> 
		extends class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!T0>
		implements Generated1479.IBase0 
{
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1951::Method0.18874()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1951::Method1.18875()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1951::Method2.18876<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated1479.IBase0::Method2<[1]>()
		ldstr "G3_C1951::Method2.MI.18877<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1951::Method3.18878<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5296() cil managed noinlining { 
		ldstr "G3_C1951::ClassMethod5296.18879()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5297<M0>() cil managed noinlining { 
		ldstr "G3_C1951::ClassMethod5297.18880<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C853<class Generated1479.BaseClass0,T0>.ClassMethod3080'<M0>() cil managed noinlining { 
		.override method instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!T0>::ClassMethod3080<[1]>()
		ldstr "G3_C1951::ClassMethod3080.MI.18881<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C853<class Generated1479.BaseClass0,T0>.ClassMethod3081'<M0>() cil managed noinlining { 
		.override method instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!T0>::ClassMethod3081<[1]>()
		ldstr "G3_C1951::ClassMethod3081.MI.18882<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public abstract Generated1479.G2_C853`2<T0, T1> 
		extends class Generated1479.G1_C15`2<!T0,class Generated1479.BaseClass1>
		implements class Generated1479.IBase2`2<class Generated1479.BaseClass0,!T1>, class Generated1479.IBase1`1<!T1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C853::Method7.12817<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C853::Method4.12818()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C853::Method5.12819()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C853::Method6.12820<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1479.IBase1`1<!T1>::Method6<[1]>()
		ldstr "G2_C853::Method6.MI.12821<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3079() cil managed noinlining { 
		ldstr "G2_C853::ClassMethod3079.12822()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3080<M0>() cil managed noinlining { 
		ldstr "G2_C853::ClassMethod3080.12823<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3081<M0>() cil managed noinlining { 
		ldstr "G2_C853::ClassMethod3081.12824<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1479.G1_C15`2<!T0,class Generated1479.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1479.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public abstract Generated1479.G1_C15`2<T0, T1> 
		implements class Generated1479.IBase2`2<!T1,!T1>, class Generated1479.IBase1`1<class Generated1479.BaseClass0> 
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
		.override method instance string class Generated1479.IBase2`2<!T1,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1479.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1479.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method5()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1479.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1479.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class interface public abstract Generated1479.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1479.Generated1479 {
	.method static void M.BaseClass0<(Generated1479.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1479.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1479.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1479.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1951.T<T0,(class Generated1479.G3_C1951`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1951.T<T0,(class Generated1479.G3_C1951`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::ClassMethod5296()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::ClassMethod5297<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1951.A<(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1951.A<(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod5296()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod5297<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1951.B<(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1951.B<(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod5296()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod5297<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.T.T<T0,T1,(class Generated1479.G2_C853`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.T.T<T0,T1,(class Generated1479.G2_C853`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.A.T<T1,(class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.A.T<T1,(class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.A.A<(class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.A.A<(class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.A.B<(class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.A.B<(class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.B.T<T1,(class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.B.T<T1,(class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.B.A<(class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.B.A<(class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C853.B.B<(class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C853.B.B<(class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::ClassMethod3079()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::ClassMethod3080<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::ClassMethod3081<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated1479.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated1479.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1479.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1479.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1479.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1479.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.T.T<T0,T1,(class Generated1479.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class Generated1479.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class Generated1479.G1_C15`2<class Generated1479.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class Generated1479.G1_C15`2<class Generated1479.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class Generated1479.G1_C15`2<class Generated1479.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class Generated1479.G1_C15`2<class Generated1479.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1479.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1479.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1479.IBase2`2<class Generated1479.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1479.IBase2`2<class Generated1479.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1479.IBase2`2<class Generated1479.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1479.IBase2`2<class Generated1479.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1479.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1479.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1479.IBase1`1<class Generated1479.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1479.IBase1`1<class Generated1479.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1479.IBase1`1<class Generated1479.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1479.IBase1`1<class Generated1479.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method6<object>()
		ldstr "G2_C853::Method6.MI.12821<System.Object>()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3081<object>()
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3080<object>()
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3079()
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method6<object>()
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod5297<object>()
		ldstr "G3_C1951::ClassMethod5297.18880<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod5296()
		ldstr "G3_C1951::ClassMethod5296.18879()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method3<object>()
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method2<object>()
		ldstr "G3_C1951::Method2.18876<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method1()
		ldstr "G3_C1951::Method1.18875()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method0()
		ldstr "G3_C1951::Method0.18874()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3081<object>()
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3080<object>()
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3079()
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method6<object>()
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1479.IBase0::Method0()
		ldstr "G3_C1951::Method0.18874()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1479.IBase0::Method1()
		ldstr "G3_C1951::Method1.18875()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1479.IBase0::Method2<object>()
		ldstr "G3_C1951::Method2.MI.18877<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1479.IBase0::Method3<object>()
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method6<object>()
		ldstr "G2_C853::Method6.MI.12821<System.Object>()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3081<object>()
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3080<object>()
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3079()
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class IBase1`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class IBase1`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method6<object>()
		ldstr "G2_C853::Method6.MI.12821<System.Object>()"
		ldstr "class IBase1`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod5297<object>()
		ldstr "G3_C1951::ClassMethod5297.18880<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod5296()
		ldstr "G3_C1951::ClassMethod5296.18879()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method3<object>()
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method2<object>()
		ldstr "G3_C1951::Method2.18876<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method1()
		ldstr "G3_C1951::Method1.18875()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method0()
		ldstr "G3_C1951::Method0.18874()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3081<object>()
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3080<object>()
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3079()
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method6<object>()
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method5()
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method4()
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		callvirt instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method7<object>()
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1479.IBase0::Method0()
		ldstr "G3_C1951::Method0.18874()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1479.IBase0::Method1()
		ldstr "G3_C1951::Method1.18875()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1479.IBase0::Method2<object>()
		ldstr "G3_C1951::Method2.MI.18877<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1479.IBase0::Method3<object>()
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
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
		newobj instance void class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1479.Generated1479::M.G1_C15.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1479.Generated1479::M.G1_C15.A.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1479.Generated1479::M.G1_C15.A.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.T.T<class Generated1479.BaseClass1,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.B.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.B.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.MI.12821<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase1.T<class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.MI.12821<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase1.A<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.A.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.A.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G2_C853.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G2_C853.A.T<class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G2_C853.A.A<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.A.T<class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.A.A<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G3_C1951::ClassMethod5296.18879()#G3_C1951::ClassMethod5297.18880<System.Object>()#G3_C1951::Method0.18874()#G3_C1951::Method1.18875()#G3_C1951::Method2.18876<System.Object>()#G3_C1951::Method3.18878<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G3_C1951.T<class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G3_C1951::ClassMethod5296.18879()#G3_C1951::ClassMethod5297.18880<System.Object>()#G3_C1951::Method0.18874()#G3_C1951::Method1.18875()#G3_C1951::Method2.18876<System.Object>()#G3_C1951::Method3.18878<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G3_C1951.A<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1951::Method0.18874()#G3_C1951::Method1.18875()#G3_C1951::Method2.MI.18877<System.Object>()#G3_C1951::Method3.18878<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase0<class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>>(!!0,string)
		newobj instance void class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1479.Generated1479::M.G1_C15.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1479.Generated1479::M.G1_C15.A.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G1_C15::Method7.4885<System.Object>()#"
		call void Generated1479.Generated1479::M.G1_C15.A.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.T.T<class Generated1479.BaseClass1,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.B.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.B.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.MI.12821<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase1.T<class Generated1479.BaseClass0,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.MI.12821<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase1.A<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.A.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase2.A.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G2_C853.T.T<class Generated1479.BaseClass0,class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G2_C853.A.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G2_C853.A.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.MI.12821<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase1.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.MI.12821<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase1.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G3_C1951::ClassMethod5296.18879()#G3_C1951::ClassMethod5297.18880<System.Object>()#G3_C1951::Method0.18874()#G3_C1951::Method1.18875()#G3_C1951::Method2.18876<System.Object>()#G3_C1951::Method3.18878<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G3_C1951.T<class Generated1479.BaseClass1,class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C853::ClassMethod3079.12822()#G3_C1951::ClassMethod3080.MI.18881<System.Object>()#G3_C1951::ClassMethod3081.MI.18882<System.Object>()#G3_C1951::ClassMethod5296.18879()#G3_C1951::ClassMethod5297.18880<System.Object>()#G3_C1951::Method0.18874()#G3_C1951::Method1.18875()#G3_C1951::Method2.18876<System.Object>()#G3_C1951::Method3.18878<System.Object>()#G2_C853::Method4.12818()#G2_C853::Method5.12819()#G2_C853::Method6.12820<System.Object>()#G2_C853::Method7.12817<System.Object>()#"
		call void Generated1479.Generated1479::M.G3_C1951.B<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1951::Method0.18874()#G3_C1951::Method1.18875()#G3_C1951::Method2.MI.18877<System.Object>()#G3_C1951::Method3.18878<System.Object>()#"
		call void Generated1479.Generated1479::M.IBase0<class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method6.MI.12821<System.Object>()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3081<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3080<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::ClassMethod3079()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod5297<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::ClassMethod5297.18880<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod5296()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::ClassMethod5296.18879()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method3<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method2<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method2.18876<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method1()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method1.18875()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method0()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method0.18874()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3081<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3080<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::ClassMethod3079()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method0()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method0.18874()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method1()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method1.18875()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method2<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method2.MI.18877<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method3<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>)
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G1_C15::Method7.4885<System.Object>()"
		ldstr "class G1_C15`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass1,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass0>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method6.MI.12821<System.Object>()"
		ldstr "class IBase1`1<class Generated1479.BaseClass0> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class IBase2`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3081<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3080<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::ClassMethod3079()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G2_C853`2<class Generated1479.BaseClass0,class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class IBase1`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class IBase1`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1479.IBase1`1<class Generated1479.BaseClass1>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method6.MI.12821<System.Object>()"
		ldstr "class IBase1`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod5297<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::ClassMethod5297.18880<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod5296()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::ClassMethod5296.18879()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method3<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method2<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method2.18876<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method1()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method1.18875()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method0()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method0.18874()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3081<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::ClassMethod3081.MI.18882<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3080<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::ClassMethod3080.MI.18881<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::ClassMethod3079()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::ClassMethod3079.12822()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method6<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method6.12820<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method5()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method5.12819()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method4()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method4.12818()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>::Method7<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G2_C853::Method7.12817<System.Object>()"
		ldstr "class G3_C1951`1<class Generated1479.BaseClass1> on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method0()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method0.18874()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method1()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method1.18875()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method2<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method2.MI.18877<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1479.IBase0::Method3<object>()
		calli default string(class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>)
		ldstr "G3_C1951::Method3.18878<System.Object>()"
		ldstr "IBase0 on type class Generated1479.G3_C1951`1<class Generated1479.BaseClass1>"
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
		call void Generated1479.Generated1479::MethodCallingTest()
		call void Generated1479.Generated1479::ConstrainedCallsTest()
		call void Generated1479.Generated1479::StructConstrainedInterfaceCallsTest()
		call void Generated1479.Generated1479::CalliTest()
		ldc.i4 100
		ret
	}
}
