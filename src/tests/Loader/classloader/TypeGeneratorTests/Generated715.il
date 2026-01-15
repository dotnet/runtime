// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated715.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated715.BaseClass1 
		extends Generated715.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated715.BaseClass0::.ctor()
		ret
	}
}
.class public Generated715.G3_C1187`1<T0> 
		extends class Generated715.G2_C247`2<class Generated715.BaseClass1,!T0>
		implements class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1187::Method7.14778<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated715.BaseClass0,class Generated715.BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<[1]>()
		ldstr "G3_C1187::Method7.MI.14779<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3762() cil managed noinlining { 
		ldstr "G3_C1187::ClassMethod3762.14780()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3763() cil managed noinlining { 
		ldstr "G3_C1187::ClassMethod3763.14781()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3764<M0>() cil managed noinlining { 
		ldstr "G3_C1187::ClassMethod3764.14782<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3765<M0>() cil managed noinlining { 
		ldstr "G3_C1187::ClassMethod3765.14783<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class public Generated715.G2_C247`2<T0, T1> 
		extends class Generated715.G1_C4`1<!T0>
		implements class Generated715.IBase2`2<class Generated715.BaseClass0,!T1>, class Generated715.IBase1`1<class Generated715.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C247::Method7.7057<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated715.BaseClass0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated715.IBase2`2<class Generated715.BaseClass0,!T1>::Method7<[1]>()
		ldstr "G2_C247::Method7.MI.7058<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C247::Method4.7059()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C247::Method5.7060()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C247::Method6.7061<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C4<T0>.ClassMethod1321'() cil managed noinlining { 
		.override method instance string class Generated715.G1_C4`1<!T0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C4<T0>.ClassMethod1322'() cil managed noinlining { 
		.override method instance string class Generated715.G1_C4`1<!T0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated715.G1_C4`1<!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated715.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated715.G1_C4`1<T0> 
		implements class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>, class Generated715.IBase1`1<class Generated715.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C4::Method7.4790<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated715.BaseClass0,class Generated715.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<[1]>()
		ldstr "G1_C4::Method7.MI.4791<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C4::Method4.4792()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated715.BaseClass1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C4::Method5.4794()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated715.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C4::Method6.4796<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated715.BaseClass1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<[1]>()
		ldstr "G1_C4::Method6.MI.4797<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1321() cil managed noinlining { 
		ldstr "G1_C4::ClassMethod1321.4798()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1322() cil managed noinlining { 
		ldstr "G1_C4::ClassMethod1322.4799()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1323<M0>() cil managed noinlining { 
		ldstr "G1_C4::ClassMethod1323.4800<"
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
.class interface public abstract Generated715.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated715.Generated715 {
	.method static void M.BaseClass0<(Generated715.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated715.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated715.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated715.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1187.T<T0,(class Generated715.G3_C1187`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1187.T<T0,(class Generated715.G3_C1187`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod3762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod3763()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod3764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::ClassMethod3765<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1187.A<(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1187.A<(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3763()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3765<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1187.B<(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1187.B<(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3762()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3763()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3764<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3765<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.T.T<T0,T1,(class Generated715.G2_C247`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.T.T<T0,T1,(class Generated715.G2_C247`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.A.T<T1,(class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.A.T<T1,(class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.A.A<(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.A.A<(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.A.B<(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.A.B<(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.B.T<T1,(class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.B.T<T1,(class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.B.A<(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.B.A<(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C247.B.B<(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C247.B.B<(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated715.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated715.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated715.IBase2`2<class Generated715.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated715.IBase2`2<class Generated715.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<class Generated715.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated715.IBase2`2<class Generated715.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated715.IBase2`2<class Generated715.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<class Generated715.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated715.IBase2`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated715.IBase2`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated715.IBase2`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated715.IBase2`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase2`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C4.T<T0,(class Generated715.G1_C4`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C4.T<T0,(class Generated715.G1_C4`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C4.A<(class Generated715.G1_C4`1<class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C4.A<(class Generated715.G1_C4`1<class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C4.B<(class Generated715.G1_C4`1<class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C4.B<(class Generated715.G1_C4`1<class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated715.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated715.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated715.IBase1`1<class Generated715.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated715.IBase1`1<class Generated715.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated715.IBase1`1<class Generated715.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated715.IBase1`1<class Generated715.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated715.G3_C1187`1<class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3765<object>()
		ldstr "G3_C1187::ClassMethod3765.14783<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3764<object>()
		ldstr "G3_C1187::ClassMethod3764.14782<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3763()
		ldstr "G3_C1187::ClassMethod3763.14781()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3762()
		ldstr "G3_C1187::ClassMethod3762.14780()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method7<object>()
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G3_C1187`1<class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3765<object>()
		ldstr "G3_C1187::ClassMethod3765.14783<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3764<object>()
		ldstr "G3_C1187::ClassMethod3764.14782<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3763()
		ldstr "G3_C1187::ClassMethod3763.14781()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3762()
		ldstr "G3_C1187::ClassMethod3762.14780()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method7<object>()
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method6<object>()
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method5()
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method4()
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		callvirt instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G1_C4`1<class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated715.G1_C4`1<class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		callvirt instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
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
		newobj instance void class Generated715.G3_C1187`1<class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.B<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.T.T<class Generated715.BaseClass1,class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.T<class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.A<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.A<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1187::ClassMethod3762.14780()#G3_C1187::ClassMethod3763.14781()#G3_C1187::ClassMethod3764.14782<System.Object>()#G3_C1187::ClassMethod3765.14783<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G3_C1187.T<class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1187::ClassMethod3762.14780()#G3_C1187::ClassMethod3763.14781()#G3_C1187::ClassMethod3764.14782<System.Object>()#G3_C1187::ClassMethod3765.14783<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G3_C1187.A<class Generated715.G3_C1187`1<class Generated715.BaseClass0>>(!!0,string)
		newobj instance void class Generated715.G3_C1187`1<class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.B<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.T.T<class Generated715.BaseClass1,class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.B<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1187::ClassMethod3762.14780()#G3_C1187::ClassMethod3763.14781()#G3_C1187::ClassMethod3764.14782<System.Object>()#G3_C1187::ClassMethod3765.14783<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G3_C1187.T<class Generated715.BaseClass1,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G3_C1187::ClassMethod3762.14780()#G3_C1187::ClassMethod3763.14781()#G3_C1187::ClassMethod3764.14782<System.Object>()#G3_C1187::ClassMethod3765.14783<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G3_C1187::Method7.14778<System.Object>()#"
		call void Generated715.Generated715::M.G3_C1187.B<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass0,class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.A<class Generated715.G3_C1187`1<class Generated715.BaseClass1>>(!!0,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.A<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.T.T<class Generated715.BaseClass0,class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.A.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.A.A<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.A<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>>(!!0,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.A<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.A.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.A.B<class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>>(!!0,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.T.T<class Generated715.BaseClass1,class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.A<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.A<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>>(!!0,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method7.MI.7058<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.T.T<class Generated715.BaseClass1,class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.T<class Generated715.BaseClass1,class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C247::ClassMethod1321.MI.7062()#G2_C247::ClassMethod1322.MI.7063()#G1_C4::ClassMethod1323.4800<System.Object>()#G2_C247::Method4.7059()#G2_C247::Method5.7060()#G2_C247::Method6.7061<System.Object>()#G2_C247::Method7.7057<System.Object>()#"
		call void Generated715.Generated715::M.G2_C247.B.B<class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>>(!!0,string)
		newobj instance void class Generated715.G1_C4`1<class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass0,class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.A<class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G1_C4`1<class Generated715.BaseClass0>>(!!0,string)
		newobj instance void class Generated715.G1_C4`1<class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.T<class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::ClassMethod1321.4798()#G1_C4::ClassMethod1322.4799()#G1_C4::ClassMethod1323.4800<System.Object>()#G1_C4::Method4.4792()#G1_C4::Method5.4794()#G1_C4::Method6.4796<System.Object>()#G1_C4::Method7.4790<System.Object>()#"
		call void Generated715.Generated715::M.G1_C4.B<class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.T.T<class Generated715.BaseClass0,class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.T<class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method7.MI.4791<System.Object>()#"
		call void Generated715.Generated715::M.IBase2.A.B<class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass1,class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.B<class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.T<class Generated715.BaseClass0,class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C4::Method4.MI.4793()#G1_C4::Method5.MI.4795()#G1_C4::Method6.MI.4797<System.Object>()#"
		call void Generated715.Generated715::M.IBase1.A<class Generated715.G1_C4`1<class Generated715.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated715.G3_C1187`1<class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3765<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::ClassMethod3765.14783<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3764<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::ClassMethod3764.14782<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3763()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::ClassMethod3763.14781()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod3762()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::ClassMethod3762.14780()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G3_C1187`1<class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3765<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::ClassMethod3765.14783<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3764<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::ClassMethod3764.14782<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3763()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::ClassMethod3763.14781()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod3762()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::ClassMethod3762.14780()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::Method7.14778<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G3_C1187`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G3_C1187`1<class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G3_C1187`1<class Generated715.BaseClass1> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G3_C1187`1<class Generated715.BaseClass1>)
		ldstr "G3_C1187::Method7.MI.14779<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G3_C1187`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass0,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>)
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method7.MI.7058<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method6.7061<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method5.7060()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method4.7059()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::Method7.7057<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1322.MI.7063()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>)
		ldstr "G2_C247::ClassMethod1321.MI.7062()"
		ldstr "class G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1> on type class Generated715.G2_C247`2<class Generated715.BaseClass1,class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G1_C4`1<class Generated715.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1323<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1322()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::ClassMethod1321()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass0>::Method7<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass0>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated715.G1_C4`1<class Generated715.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1323<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1323.4800<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1322()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1322.4799()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::ClassMethod1321()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::ClassMethod1321.4798()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method6.4796<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method5.4794()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method4.4792()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated715.G1_C4`1<class Generated715.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated715.G1_C4`1<class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method7.4790<System.Object>()"
		ldstr "class G1_C4`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1>::Method7<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method7.MI.4791<System.Object>()"
		ldstr "class IBase2`2<class Generated715.BaseClass0,class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method4()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method5()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass1>::Method6<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass1> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method4()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method4.MI.4793()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method5()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method5.MI.4795()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated715.IBase1`1<class Generated715.BaseClass0>::Method6<object>()
		calli default string(class Generated715.G1_C4`1<class Generated715.BaseClass1>)
		ldstr "G1_C4::Method6.MI.4797<System.Object>()"
		ldstr "class IBase1`1<class Generated715.BaseClass0> on type class Generated715.G1_C4`1<class Generated715.BaseClass1>"
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
		call void Generated715.Generated715::MethodCallingTest()
		call void Generated715.Generated715::ConstrainedCallsTest()
		call void Generated715.Generated715::StructConstrainedInterfaceCallsTest()
		call void Generated715.Generated715::CalliTest()
		ldc.i4 100
		ret
	}
}
