// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1499.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1499.BaseClass1 
		extends Generated1499.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1499.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1499.G3_C1971`1<T0> 
		extends class Generated1499.G2_C865`2<!T0,class Generated1499.BaseClass1>
		implements class Generated1499.IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1971::Method4.18991()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1971::Method5.18992()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1971::Method6.18993<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1499.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G3_C1971::Method6.MI.18994<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5338<M0>() cil managed noinlining { 
		ldstr "G3_C1971::ClassMethod5338.18995<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod5339<M0>() cil managed noinlining { 
		ldstr "G3_C1971::ClassMethod5339.18996<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C865<T0,class Generated1499.BaseClass1>.ClassMethod3103'() cil managed noinlining { 
		.override method instance string class Generated1499.G2_C865`2<!T0,class Generated1499.BaseClass1>::ClassMethod3103()
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1499.G2_C865`2<!T0,class Generated1499.BaseClass1>::.ctor()
		ret
	}
}
.class public Generated1499.G2_C865`2<T0, T1> 
		extends class Generated1499.G1_C15`2<!T1,!T0>
		implements class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>, Generated1499.IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C865::Method7.12906<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C865::Method0.12907()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1499.IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C865::Method1.12909()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1499.IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C865::Method2.12911<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1499.IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated1499.IBase0::Method2<[1]>()
		ldstr "G2_C865::Method2.MI.12912<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C865::Method3.12913<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1499.IBase0.Method3'<M0>() cil managed noinlining { 
		.override method instance string Generated1499.IBase0::Method3<[1]>()
		ldstr "G2_C865::Method3.MI.12914<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3103() cil managed noinlining { 
		ldstr "G2_C865::ClassMethod3103.12915()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3104() cil managed noinlining { 
		ldstr "G2_C865::ClassMethod3104.12916()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3105<M0>() cil managed noinlining { 
		ldstr "G2_C865::ClassMethod3105.12917<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1499.G1_C15`2<!T1,!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated1499.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract Generated1499.G1_C15`2<T0, T1> 
		implements class Generated1499.IBase2`2<!T1,!T1>, class Generated1499.IBase1`1<class Generated1499.BaseClass0> 
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
		.override method instance string class Generated1499.IBase2`2<!T1,!T1>::Method7<[1]>()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1499.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C15::Method5.4889()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1499.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1499.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1499.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class interface public abstract Generated1499.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1499.Generated1499 {
	.method static void M.BaseClass0<(Generated1499.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1499.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1499.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1499.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1971.T<T0,(class Generated1499.G3_C1971`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1971.T<T0,(class Generated1499.G3_C1971`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::ClassMethod5338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::ClassMethod5339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1971.A<(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1971.A<(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod5338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod5339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1971.B<(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1971.B<(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod5338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod5339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.T.T<T0,T1,(class Generated1499.G2_C865`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.T.T<T0,T1,(class Generated1499.G2_C865`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.A.T<T1,(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.A.T<T1,(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.A.A<(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.A.A<(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.A.B<(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.A.B<(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.B.T<T1,(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.B.T<T1,(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.B.A<(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.B.A<(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C865.B.B<(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C865.B.B<(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3103()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3104()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1499.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1499.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1499.IBase1`1<class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1499.IBase1`1<class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1499.IBase1`1<class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1499.IBase1`1<class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.T.T<T0,T1,(class Generated1499.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.T.T<T0,T1,(class Generated1499.G1_C15`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.T<T1,(class Generated1499.G1_C15`2<class Generated1499.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.T<T1,(class Generated1499.G1_C15`2<class Generated1499.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.A<(class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.A<(class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.A.B<(class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.A.B<(class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.T<T1,(class Generated1499.G1_C15`2<class Generated1499.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.T<T1,(class Generated1499.G1_C15`2<class Generated1499.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.A<(class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.A<(class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C15.B.B<(class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C15.B.B<(class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1499.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1499.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1499.IBase2`2<class Generated1499.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1499.IBase2`2<class Generated1499.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1499.IBase2`2<class Generated1499.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1499.IBase2`2<class Generated1499.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated1499.IBase0<(Generated1499.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated1499.IBase0<(Generated1499.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1499.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1499.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1499.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1499.IBase0::Method3<object>()
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
		newobj instance void class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G3_C1971::Method6.MI.18994<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3103()
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method2<object>()
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method3<object>()
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod5339<object>()
		ldstr "G3_C1971::ClassMethod5339.18996<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod5338<object>()
		ldstr "G3_C1971::ClassMethod5338.18995<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G3_C1971::Method6.18993<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3103()
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G3_C1971::Method6.MI.18994<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3103()
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method2<object>()
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method3<object>()
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod5339<object>()
		ldstr "G3_C1971::ClassMethod5339.18996<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod5338<object>()
		ldstr "G3_C1971::ClassMethod5338.18995<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G3_C1971::Method6.18993<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method5()
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method4()
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3103()
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method4()
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class IBase1`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method5()
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class IBase1`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G3_C1971::Method6.MI.18994<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3103()
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method2<object>()
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method3<object>()
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3103()
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method2<object>()
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method3<object>()
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3103()
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method2<object>()
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method3<object>()
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3104()
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3103()
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method3<object>()
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method2<object>()
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method1()
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method0()
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		callvirt instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1499.IBase0::Method0()
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method1()
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method2<object>()
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1499.IBase0::Method3<object>()
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
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
		newobj instance void class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.T<class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.A<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.A<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.MI.18994<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.MI.18994<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.A<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.A.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.A.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method0.MI.12908()#G2_C865::Method1.MI.12910()#G2_C865::Method2.MI.12912<System.Object>()#G2_C865::Method3.MI.12914<System.Object>()#"
		call void Generated1499.Generated1499::M.Generated1499.IBase0<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G3_C1971::ClassMethod5338.18995<System.Object>()#G3_C1971::ClassMethod5339.18996<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.18993<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G3_C1971.T<class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G3_C1971::ClassMethod5338.18995<System.Object>()#G3_C1971::ClassMethod5339.18996<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.18993<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G3_C1971.A<class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>>(!!0,string)
		newobj instance void class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.MI.18994<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass0,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.MI.18994<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.A<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.B.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.B.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method0.MI.12908()#G2_C865::Method1.MI.12910()#G2_C865::Method2.MI.12912<System.Object>()#G2_C865::Method3.MI.12914<System.Object>()#"
		call void Generated1499.Generated1499::M.Generated1499.IBase0<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G3_C1971::ClassMethod5338.18995<System.Object>()#G3_C1971::ClassMethod5339.18996<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.18993<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G3_C1971.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::ClassMethod3103.MI.18997()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G3_C1971::ClassMethod5338.18995<System.Object>()#G3_C1971::ClassMethod5339.18996<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.18993<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G3_C1971.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.MI.18994<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass1,class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1971::Method4.18991()#G3_C1971::Method5.18992()#G3_C1971::Method6.MI.18994<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.B<class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>>(!!0,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.A.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.A.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.A.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.A.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method0.MI.12908()#G2_C865::Method1.MI.12910()#G2_C865::Method2.MI.12912<System.Object>()#G2_C865::Method3.MI.12914<System.Object>()#"
		call void Generated1499.Generated1499::M.Generated1499.IBase0<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>>(!!0,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method7.MI.4886<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.A.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.A.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method0.MI.12908()#G2_C865::Method1.MI.12910()#G2_C865::Method2.MI.12912<System.Object>()#G2_C865::Method3.MI.12914<System.Object>()#"
		call void Generated1499.Generated1499::M.Generated1499.IBase0<class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>>(!!0,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.A.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.A.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.B.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.B.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method0.MI.12908()#G2_C865::Method1.MI.12910()#G2_C865::Method2.MI.12912<System.Object>()#G2_C865::Method3.MI.12914<System.Object>()#"
		call void Generated1499.Generated1499::M.Generated1499.IBase0<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>>(!!0,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G1_C15.B.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.B.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.T<class Generated1499.BaseClass0,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C15::Method4.MI.4888()#G1_C15::Method5.MI.4890()#G1_C15::Method6.MI.4892<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase1.A<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.T.T<class Generated1499.BaseClass0,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.IBase2.A.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.T.T<class Generated1499.BaseClass1,class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.B.T<class Generated1499.BaseClass1,class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C865::ClassMethod3103.12915()#G2_C865::ClassMethod3104.12916()#G2_C865::ClassMethod3105.12917<System.Object>()#G2_C865::Method0.12907()#G2_C865::Method1.12909()#G2_C865::Method2.12911<System.Object>()#G2_C865::Method3.12913<System.Object>()#G1_C15::Method4.4887()#G1_C15::Method5.4889()#G1_C15::Method6.4891<System.Object>()#G2_C865::Method7.12906<System.Object>()#"
		call void Generated1499.Generated1499::M.G2_C865.B.B<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C865::Method0.MI.12908()#G2_C865::Method1.MI.12910()#G2_C865::Method2.MI.12912<System.Object>()#G2_C865::Method3.MI.12914<System.Object>()#"
		call void Generated1499.Generated1499::M.Generated1499.IBase0<class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::Method6.MI.18994<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3104()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3103()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method3<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method2<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method1()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method0()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method0()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method1()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method2<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method3<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod5339<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::ClassMethod5339.18996<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod5338<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::ClassMethod5338.18995<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::Method6.18993<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3105<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3104()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::ClassMethod3103()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method3<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method2<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method1()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method0()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method6.MI.18994<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3104()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3103()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method3<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method2<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method1()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method0()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method0()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method1()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method2<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method3<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod5339<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::ClassMethod5339.18996<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod5338<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::ClassMethod5338.18995<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method6.18993<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3105<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3104()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::ClassMethod3103()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::ClassMethod3103.MI.18997()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method3<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method2<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method1()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method0()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G3_C1971`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method4.18991()"
		ldstr "class IBase1`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method5.18992()"
		ldstr "class IBase1`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>)
		ldstr "G3_C1971::Method6.MI.18994<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass1> on type class Generated1499.G3_C1971`1<class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3105<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3104()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::ClassMethod3103()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method7.MI.4886<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3104()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::ClassMethod3103()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3105<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3104()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::ClassMethod3103()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>)
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G1_C15`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.MI.4888()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.MI.4890()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase1`1<class Generated1499.BaseClass0>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.MI.4892<System.Object>()"
		ldstr "class IBase1`1<class Generated1499.BaseClass0> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1499.IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class IBase2`2<class Generated1499.BaseClass0,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3105<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3105.12917<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3104()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3104.12916()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::ClassMethod3103()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::ClassMethod3103.12915()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.12913<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.12911<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.12909()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.12907()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method7<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method7.12906<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method6<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method6.4891<System.Object>()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method5()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method5.4889()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>::Method4()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G1_C15::Method4.4887()"
		ldstr "class G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1> on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method0()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method0.MI.12908()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method1()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method1.MI.12910()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method2<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method2.MI.12912<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1499.IBase0::Method3<object>()
		calli default string(class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>)
		ldstr "G2_C865::Method3.MI.12914<System.Object>()"
		ldstr "Generated1499.IBase0 on type class Generated1499.G2_C865`2<class Generated1499.BaseClass1,class Generated1499.BaseClass1>"
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
				.maxstack  10
		call void Generated1499.Generated1499::MethodCallingTest()
		call void Generated1499.Generated1499::ConstrainedCallsTest()
		call void Generated1499.Generated1499::StructConstrainedInterfaceCallsTest()
		call void Generated1499.Generated1499::CalliTest()
		ldc.i4 100
		ret
	}
}
