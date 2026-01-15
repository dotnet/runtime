// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1096.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1096.BaseClass1 
		extends Generated1096.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1096.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1096.G3_C1568`1<T0> 
		extends class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!T0>
		implements class Generated1096.IBase1`1<class Generated1096.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1568::Method4.16836()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1096.BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method4()
		ldstr "G3_C1568::Method4.MI.16837()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1568::Method5.16838()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1096.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method5()
		ldstr "G3_C1568::Method5.MI.16839()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1568::Method6.16840<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1096.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method6<[1]>()
		ldstr "G3_C1568::Method6.MI.16841<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4521() cil managed noinlining { 
		ldstr "G3_C1568::ClassMethod4521.16842()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4522() cil managed noinlining { 
		ldstr "G3_C1568::ClassMethod4522.16843()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C574<class Generated1096.BaseClass0,T0>.ClassMethod2521'() cil managed noinlining { 
		.override method instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!T0>::ClassMethod2521()
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!T0>::.ctor()
		ret
	}
}
.class public Generated1096.G2_C574`2<T0, T1> 
		extends class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!T1>
		implements class Generated1096.IBase1`1<class Generated1096.BaseClass0>, class Generated1096.IBase2`2<!T1,!T1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C574::Method4.10288()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C574::Method5.10289()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1096.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.MI.10290()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C574::Method6.10291<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C574::Method7.10292<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2521() cil managed noinlining { 
		ldstr "G2_C574::ClassMethod2521.10293()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C11<class Generated1096.BaseClass0,T1>.ClassMethod1344'<M0>() cil managed noinlining { 
		.override method instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!T1>::ClassMethod1344<[1]>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1096.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public Generated1096.G1_C11`2<T0, T1> 
		implements class Generated1096.IBase2`2<class Generated1096.BaseClass1,!T1>, class Generated1096.IBase1`1<class Generated1096.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method7.4854<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1096.BaseClass1,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,!T1>::Method7<[1]>()
		ldstr "G1_C11::Method7.MI.4855<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C11::Method4.4856()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1096.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C11::Method5.4858()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C11::Method6.4859<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1096.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<[1]>()
		ldstr "G1_C11::Method6.MI.4860<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1344<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1344.4861<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1345<M0>() cil managed noinlining { 
		ldstr "G1_C11::ClassMethod1345.4862<"
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
.class interface public abstract Generated1096.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1096.Generated1096 {
	.method static void M.BaseClass0<(Generated1096.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1096.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1096.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1096.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1568.T<T0,(class Generated1096.G3_C1568`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1568.T<T0,(class Generated1096.G3_C1568`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::ClassMethod4521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::ClassMethod4522()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1568.A<(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1568.A<(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod4521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod4522()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1568.B<(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1568.B<(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod4521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod4522()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.T.T<T0,T1,(class Generated1096.G2_C574`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.T.T<T0,T1,(class Generated1096.G2_C574`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.A.T<T1,(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.A.T<T1,(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.A.A<(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.A.A<(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.A.B<(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.A.B<(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.B.T<T1,(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.B.T<T1,(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.B.A<(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.B.A<(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C574.B.B<(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C574.B.B<(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod2521()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1096.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1096.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1096.IBase1`1<class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1096.IBase1`1<class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1096.IBase1`1<class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1096.IBase1`1<class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.T.T<T0,T1,(class Generated1096.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.T.T<T0,T1,(class Generated1096.G1_C11`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<!!T0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<!!T0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.T<T1,(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.T<T1,(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.A<(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.A<(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.A.B<(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.A.B<(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.T<T1,(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.T<T1,(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.A<(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.A<(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C11.B.B<(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C11.B.B<(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1096.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1096.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1096.IBase2`2<class Generated1096.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1096.IBase2`2<class Generated1096.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1096.IBase2`2<class Generated1096.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1096.IBase2`2<class Generated1096.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod2521()
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod4522()
		ldstr "G3_C1568::ClassMethod4522.16843()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod4521()
		ldstr "G3_C1568::ClassMethod4521.16842()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G3_C1568::Method6.16840<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G3_C1568::Method5.16838()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G3_C1568::Method4.16836()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod2521()
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method4()
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method5()
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod2521()
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod4522()
		ldstr "G3_C1568::ClassMethod4522.16843()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod4521()
		ldstr "G3_C1568::ClassMethod4521.16842()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G3_C1568::Method6.16840<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method5()
		ldstr "G3_C1568::Method5.16838()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method4()
		ldstr "G3_C1568::Method4.16836()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod2521()
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method4()
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method5()
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod2521()
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod2521()
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod2521()
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod2521()
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method5()
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method4()
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method6<object>()
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method4()
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		callvirt instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
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
		newobj instance void class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G3_C1568::ClassMethod4521.16842()#G3_C1568::ClassMethod4522.16843()#G3_C1568::Method4.16836()#G3_C1568::Method5.16838()#G3_C1568::Method6.16840<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G3_C1568.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G3_C1568::ClassMethod4521.16842()#G3_C1568::ClassMethod4522.16843()#G3_C1568::Method4.16836()#G3_C1568::Method5.16838()#G3_C1568::Method6.16840<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G3_C1568.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>>(!!0,string)
		newobj instance void class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G3_C1568::ClassMethod4521.16842()#G3_C1568::ClassMethod4522.16843()#G3_C1568::Method4.16836()#G3_C1568::Method5.16838()#G3_C1568::Method6.16840<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G3_C1568.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G3_C1568::ClassMethod2521.MI.16844()#G3_C1568::ClassMethod4521.16842()#G3_C1568::ClassMethod4522.16843()#G3_C1568::Method4.16836()#G3_C1568::Method5.16838()#G3_C1568::Method6.16840<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G3_C1568.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass1,class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1568::Method4.MI.16837()#G3_C1568::Method5.MI.16839()#G3_C1568::Method6.MI.16841<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.B<class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>>(!!0,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.B.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.B.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::Method4.10288()#G1_C11::Method5.4858()#G2_C574::Method6.10291<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method4.10288()#G2_C574::Method5.MI.10290()#G2_C574::Method6.10291<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.B.T<class Generated1096.BaseClass1,class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G2_C574::ClassMethod2521.10293()#G2_C574::Method4.10288()#G2_C574::Method5.10289()#G2_C574::Method6.10291<System.Object>()#G2_C574::Method7.10292<System.Object>()#"
		call void Generated1096.Generated1096::M.G2_C574.B.B<class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>>(!!0,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.A.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>>(!!0,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.B.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.B.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>>(!!0,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.B.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()#G1_C11::ClassMethod1345.4862<System.Object>()#G1_C11::Method4.4856()#G1_C11::Method5.4858()#G1_C11::Method6.4859<System.Object>()#G1_C11::Method7.4854<System.Object>()#"
		call void Generated1096.Generated1096::M.G1_C11.B.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass1,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.B.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.T<class Generated1096.BaseClass0,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method4.MI.4857()#G1_C11::Method5.4858()#G1_C11::Method6.MI.4860<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase1.A<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.T.T<class Generated1096.BaseClass0,class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.T<class Generated1096.BaseClass1,class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C11::Method7.MI.4855<System.Object>()#"
		call void Generated1096.Generated1096::M.IBase2.A.B<class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod2521()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod4522()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::ClassMethod4522.16843()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod4521()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::ClassMethod4521.16842()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method6.16840<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method5.16838()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method4.16836()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod2521()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>)
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod2521()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod4522()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::ClassMethod4522.16843()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod4521()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::ClassMethod4521.16842()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method6.16840<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method5.16838()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method4.16836()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod2521()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::ClassMethod2521.MI.16844()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G3_C1568`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method4.MI.16837()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method5.MI.16839()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>)
		ldstr "G3_C1568::Method6.MI.16841<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass1> on type class Generated1096.G3_C1568`1<class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod2521()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod2521()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod2521()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method5.MI.10290()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod2521()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod2521.10293()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method7.10292<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method6.10291<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method5.10289()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::Method4.10288()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G2_C574::ClassMethod1344.MI.10294<System.Object>()"
		ldstr "class G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G2_C574`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1345<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::ClassMethod1344<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1345<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1345.4862<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::ClassMethod1344<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::ClassMethod1344.4861<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method6.4859<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method4.4856()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.4854<System.Object>()"
		ldstr "class G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method4()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method4.MI.4857()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method5()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method5.4858()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase1`1<class Generated1096.BaseClass0>::Method6<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method6.MI.4860<System.Object>()"
		ldstr "class IBase1`1<class Generated1096.BaseClass0> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1096.IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1>::Method7<object>()
		calli default string(class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>)
		ldstr "G1_C11::Method7.MI.4855<System.Object>()"
		ldstr "class IBase2`2<class Generated1096.BaseClass0,class Generated1096.BaseClass1> on type class Generated1096.G1_C11`2<class Generated1096.BaseClass1,class Generated1096.BaseClass1>"
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
		call void Generated1096.Generated1096::MethodCallingTest()
		call void Generated1096.Generated1096::ConstrainedCallsTest()
		call void Generated1096.Generated1096::StructConstrainedInterfaceCallsTest()
		call void Generated1096.Generated1096::CalliTest()
		ldc.i4 100
		ret
	}
}
