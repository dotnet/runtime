// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated974.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated974.BaseClass1 
		extends Generated974.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated974.BaseClass0::.ctor()
		ret
	}
}
.class public Generated974.G3_C1446`1<T0> 
		extends class Generated974.G2_C474`2<class Generated974.BaseClass0,!T0>
		implements class Generated974.IBase2`2<!T0,class Generated974.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1446::Method7.16218<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated974.G2_C474`2<class Generated974.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public abstract Generated974.G2_C474`2<T0, T1> 
		extends class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		implements class Generated974.IBase1`1<!T0>, class Generated974.IBase2`2<!T1,class Generated974.BaseClass0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C474::Method4.9297()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C474::Method5.9298()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated974.IBase1`1<!T0>::Method5()
		ldstr "G2_C474::Method5.MI.9299()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C474::Method6.9300<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated974.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C474::Method6.MI.9301<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C474::Method7.9302<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,class Generated974.BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated974.IBase2`2<!T1,class Generated974.BaseClass0>::Method7<[1]>()
		ldstr "G2_C474::Method7.MI.9303<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2307() cil managed noinlining { 
		ldstr "G2_C474::ClassMethod2307.9304()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2308<M0>() cil managed noinlining { 
		ldstr "G2_C474::ClassMethod2308.9305<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2309<M0>() cil managed noinlining { 
		ldstr "G2_C474::ClassMethod2309.9306<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class Generated974.BaseClass0,class Generated974.BaseClass1>.ClassMethod1338'<M0>() cil managed noinlining { 
		.override method instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<[1]>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<class Generated974.BaseClass0,class Generated974.BaseClass1>.ClassMethod1339'<M0>() cil managed noinlining { 
		.override method instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<[1]>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated974.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated974.G1_C9`2<T0, T1> 
		implements class Generated974.IBase2`2<class Generated974.BaseClass0,!T0>, class Generated974.IBase1`1<class Generated974.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C9::Method7.4833<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C9::Method4.4834()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated974.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method4()
		ldstr "G1_C9::Method4.MI.4835()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C9::Method5.4836()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated974.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method5()
		ldstr "G1_C9::Method5.MI.4837()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C9::Method6.4838<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated974.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method6<[1]>()
		ldstr "G1_C9::Method6.MI.4839<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1336() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1336.4840()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1337() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1337.4841()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1338<M0>() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1338.4842<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1339<M0>() cil managed noinlining { 
		ldstr "G1_C9::ClassMethod1339.4843<"
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
.class interface public abstract Generated974.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated974.Generated974 {
	.method static void M.BaseClass0<(Generated974.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated974.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated974.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated974.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1446.T<T0,(class Generated974.G3_C1446`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1446.T<T0,(class Generated974.G3_C1446`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1446.A<(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1446.A<(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1446.B<(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1446.B<(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.T.T<T0,T1,(class Generated974.G2_C474`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.T.T<T0,T1,(class Generated974.G2_C474`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.A.T<T1,(class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.A.T<T1,(class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.A.A<(class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.A.A<(class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.A.B<(class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.A.B<(class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.B.T<T1,(class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.B.T<T1,(class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.B.A<(class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.B.A<(class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C474.B.B<(class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C474.B.B<(class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod2307()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod2308<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod2309<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G2_C474`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated974.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated974.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated974.IBase2`2<class Generated974.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated974.IBase2`2<class Generated974.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<class Generated974.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated974.IBase2`2<class Generated974.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated974.IBase2`2<class Generated974.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<class Generated974.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.T.T<T0,T1,(class Generated974.G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.T.T<T0,T1,(class Generated974.G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.T<T1,(class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.T<T1,(class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.A<(class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.A<(class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.B<(class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.B<(class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.T<T1,(class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.T<T1,(class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.A<(class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.A<(class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.B<(class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.B<(class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.G1_C9`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated974.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated974.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated974.IBase1`1<class Generated974.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated974.IBase1`1<class Generated974.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated974.IBase1`1<class Generated974.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated974.IBase1`1<class Generated974.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<class Generated974.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<class Generated974.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated974.IBase1`1<class Generated974.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated974.G3_C1446`1<class Generated974.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		ldstr "G2_C474::Method7.MI.9303<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method5()
		ldstr "G2_C474::Method5.MI.9299()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method6<object>()
		ldstr "G2_C474::Method6.MI.9301<System.Object>()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2309<object>()
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2308<object>()
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2307()
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method6<object>()
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method5()
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method7<object>()
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2309<object>()
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2308<object>()
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2307()
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method6<object>()
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method5()
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated974.G3_C1446`1<class Generated974.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		ldstr "G2_C474::Method7.MI.9303<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method5()
		ldstr "G2_C474::Method5.MI.9299()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method6<object>()
		ldstr "G2_C474::Method6.MI.9301<System.Object>()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2309<object>()
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2308<object>()
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2307()
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		callvirt instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method7<object>()
		ldstr "G2_C474::Method7.MI.9303<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method7<object>()
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method7<object>()
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2309<object>()
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2308<object>()
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2307()
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method6<object>()
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method5()
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method4()
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		callvirt instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
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
		newobj instance void class Generated974.G3_C1446`1<class Generated974.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::Method4.9297()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G1_C9.T.T<class Generated974.BaseClass0,class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::Method4.9297()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G1_C9.A.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::Method4.9297()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G1_C9.A.B<class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.T.T<class Generated974.BaseClass0,class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.A<class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C474::Method4.9297()#G2_C474::Method5.MI.9299()#G2_C474::Method6.MI.9301<System.Object>()#"
		call void Generated974.Generated974::M.IBase1.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C474::Method4.9297()#G2_C474::Method5.MI.9299()#G2_C474::Method6.MI.9301<System.Object>()#"
		call void Generated974.Generated974::M.IBase1.A<class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.T.T<class Generated974.BaseClass0,class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.B<class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G2_C474.T.T<class Generated974.BaseClass0,class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G2_C474.A.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G2_C474.A.A<class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.G3_C1446.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.G3_C1446.A<class Generated974.G3_C1446`1<class Generated974.BaseClass0>>(!!0,string)
		newobj instance void class Generated974.G3_C1446`1<class Generated974.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::Method4.9297()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G1_C9.T.T<class Generated974.BaseClass0,class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::Method4.9297()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G1_C9.A.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::Method4.9297()#G1_C9::Method5.4836()#G1_C9::Method6.4838<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G1_C9.A.B<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.T.T<class Generated974.BaseClass0,class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.A<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C474::Method4.9297()#G2_C474::Method5.MI.9299()#G2_C474::Method6.MI.9301<System.Object>()#"
		call void Generated974.Generated974::M.IBase1.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C474::Method4.9297()#G2_C474::Method5.MI.9299()#G2_C474::Method6.MI.9301<System.Object>()#"
		call void Generated974.Generated974::M.IBase1.A<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.T.T<class Generated974.BaseClass0,class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.A.B<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G2_C474.T.T<class Generated974.BaseClass0,class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G2_C474.A.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G2_C474::Method7.9302<System.Object>()#"
		call void Generated974.Generated974::M.G2_C474.A.B<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.T.T<class Generated974.BaseClass1,class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.B.T<class Generated974.BaseClass0,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C474::Method7.MI.9303<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.B.A<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.T.T<class Generated974.BaseClass1,class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.B.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.IBase2.B.B<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.G3_C1446.T<class Generated974.BaseClass1,class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C474::ClassMethod1338.MI.9307<System.Object>()#G2_C474::ClassMethod1339.MI.9308<System.Object>()#G2_C474::ClassMethod2307.9304()#G2_C474::ClassMethod2308.9305<System.Object>()#G2_C474::ClassMethod2309.9306<System.Object>()#G2_C474::Method4.9297()#G2_C474::Method5.9298()#G2_C474::Method6.9300<System.Object>()#G3_C1446::Method7.16218<System.Object>()#"
		call void Generated974.Generated974::M.G3_C1446.B<class Generated974.G3_C1446`1<class Generated974.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated974.G3_C1446`1<class Generated974.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method7.MI.9303<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method5.MI.9299()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method6.MI.9301<System.Object>()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2309<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2308<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod2307()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1339<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1338<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1337()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::ClassMethod1336()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2309<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2308<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod2307()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1339<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1338<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1337()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass0>::ClassMethod1336()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass0>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated974.G3_C1446`1<class Generated974.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::Method5.4836()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G1_C9`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method7.MI.9303<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method5.MI.9299()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase1`1<class Generated974.BaseClass0>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method6.MI.9301<System.Object>()"
		ldstr "class IBase1`1<class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2309<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2308<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod2307()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method7.9302<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1337()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1>::ClassMethod1336()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C474`2<class Generated974.BaseClass0,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method7.MI.9303<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass0> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated974.IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class IBase2`2<class Generated974.BaseClass1,class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method7<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G3_C1446::Method7.16218<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2309<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod2309.9306<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2308<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod2308.9305<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod2307()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod2307.9304()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method6<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method6.9300<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method5()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method5.9298()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::Method4()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::Method4.9297()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod1339.MI.9308<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G2_C474::ClassMethod1338.MI.9307<System.Object>()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1337()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated974.G3_C1446`1<class Generated974.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated974.G3_C1446`1<class Generated974.BaseClass1>::ClassMethod1336()
		calli default string(class Generated974.G3_C1446`1<class Generated974.BaseClass1>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1446`1<class Generated974.BaseClass1> on type class Generated974.G3_C1446`1<class Generated974.BaseClass1>"
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
		call void Generated974.Generated974::MethodCallingTest()
		call void Generated974.Generated974::ConstrainedCallsTest()
		call void Generated974.Generated974::StructConstrainedInterfaceCallsTest()
		call void Generated974.Generated974::CalliTest()
		ldc.i4 100
		ret
	}
}
