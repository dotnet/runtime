// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1056.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1056.BaseClass1 
		extends Generated1056.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1056.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1056.G3_C1528`1<T0> 
		extends class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		implements class Generated1056.IBase1`1<class Generated1056.BaseClass0> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1528::Method4.16642()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1056.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G3_C1528::Method4.MI.16643()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1528::Method5.16644()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1056.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G3_C1528::Method5.MI.16645()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1528::Method6.16646<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4453() cil managed noinlining { 
		ldstr "G3_C1528::ClassMethod4453.16647()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C547<class Generated1056.BaseClass1,class Generated1056.BaseClass0>.ClassMethod2463'() cil managed noinlining { 
		.override method instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C547<class Generated1056.BaseClass1,class Generated1056.BaseClass0>.ClassMethod2464'<M0>() cil managed noinlining { 
		.override method instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<[1]>()
		ldstr "G3_C1528::ClassMethod2464.MI.16649<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated1056.G2_C547`2<T0, T1> 
		extends class Generated1056.G1_C10`2<!T0,!T0>
		implements class Generated1056.IBase2`2<class Generated1056.BaseClass0,!T1>, class Generated1056.IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C547::Method7.10031<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1056.BaseClass0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,!T1>::Method7<[1]>()
		ldstr "G2_C547::Method7.MI.10032<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C547::Method4.10033()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C547::Method5.10034()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C547::Method6.10035<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1056.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G2_C547::Method6.MI.10036<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2463() cil managed noinlining { 
		ldstr "G2_C547::ClassMethod2463.10037()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2464<M0>() cil managed noinlining { 
		ldstr "G2_C547::ClassMethod2464.10038<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<T0,T0>.ClassMethod1342'<M0>() cil managed noinlining { 
		.override method instance string class Generated1056.G1_C10`2<!T0,!T0>::ClassMethod1342<[1]>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1056.G1_C10`2<!T0,!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated1056.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public Generated1056.G1_C10`2<T0, T1> 
		implements class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>, class Generated1056.IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C10::Method7.4844<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C10::Method4.4845()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1056.IBase1`1<!T0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C10::Method5.4847()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C10::Method6.4848<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1056.IBase1`1<!T0>::Method6<[1]>()
		ldstr "G1_C10::Method6.MI.4849<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1340() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1340.4850()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1341() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1341.4851()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1342<M0>() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1342.4852<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1343<M0>() cil managed noinlining { 
		ldstr "G1_C10::ClassMethod1343.4853<"
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
.class interface public abstract Generated1056.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1056.Generated1056 {
	.method static void M.BaseClass0<(Generated1056.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1056.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1056.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1056.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1528.T<T0,(class Generated1056.G3_C1528`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1528.T<T0,(class Generated1056.G3_C1528`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::ClassMethod4453()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1528.A<(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1528.A<(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod4453()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1528.B<(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 16
		.locals init (string[] actualResults)
		ldc.i4.s    11
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1528.B<(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 11
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod4453()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.T.T<T0,T1,(class Generated1056.G2_C547`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.T.T<T0,T1,(class Generated1056.G2_C547`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.A.T<T1,(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.A.T<T1,(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.A.A<(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.A.A<(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.A.B<(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.A.B<(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.B.T<T1,(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.B.T<T1,(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.B.A<(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.B.A<(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C547.B.B<(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C547.B.B<(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod2463()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod2464<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1056.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1056.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1056.IBase1`1<class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1056.IBase1`1<class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1056.IBase1`1<class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1056.IBase1`1<class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.T.T<T0,T1,(class Generated1056.G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.T.T<T0,T1,(class Generated1056.G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.T<T1,(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.T<T1,(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.A<(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.A<(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.B<(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.B<(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.T<T1,(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.T<T1,(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.A<(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.A<(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.B<(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.B<(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1056.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1056.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1056.IBase2`2<class Generated1056.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1056.IBase2`2<class Generated1056.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1056.IBase2`2<class Generated1056.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1056.IBase2`2<class Generated1056.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G3_C1528::Method4.MI.16643()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G3_C1528::Method5.MI.16645()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod4453()
		ldstr "G3_C1528::ClassMethod4453.16647()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod2464<object>()
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod2463()
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G3_C1528::Method4.MI.16643()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G3_C1528::Method5.MI.16645()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod4453()
		ldstr "G3_C1528::ClassMethod4453.16647()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod2464<object>()
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod2463()
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod2463()
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod2464<object>()
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod2463()
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod2464<object>()
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod2463()
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		callvirt instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
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
		newobj instance void class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1528::Method4.MI.16643()#G3_C1528::Method5.MI.16645()#G3_C1528::Method6.16646<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1528::Method4.MI.16643()#G3_C1528::Method5.MI.16645()#G3_C1528::Method6.16646<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::ClassMethod4453.16647()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G3_C1528.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::ClassMethod4453.16647()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G3_C1528.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>>(!!0,string)
		newobj instance void class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1528::Method4.MI.16643()#G3_C1528::Method5.MI.16645()#G3_C1528::Method6.16646<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1528::Method4.MI.16643()#G3_C1528::Method5.MI.16645()#G3_C1528::Method6.16646<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass0,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.A<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::ClassMethod4453.16647()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G3_C1528.T<class Generated1056.BaseClass1,class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G3_C1528::ClassMethod2463.MI.16648()#G3_C1528::ClassMethod2464.MI.16649<System.Object>()#G3_C1528::ClassMethod4453.16647()#G3_C1528::Method4.16642()#G3_C1528::Method5.16644()#G3_C1528::Method6.16646<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G3_C1528.B<class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>>(!!0,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.A.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.A.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.A.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.A.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method7.MI.10032<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.MI.10036<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.T<class Generated1056.BaseClass1,class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G2_C547::ClassMethod1342.MI.10039<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C547::ClassMethod2463.10037()#G2_C547::ClassMethod2464.10038<System.Object>()#G2_C547::Method4.10033()#G2_C547::Method5.10034()#G2_C547::Method6.10035<System.Object>()#G2_C547::Method7.10031<System.Object>()#"
		call void Generated1056.Generated1056::M.G2_C547.B.B<class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.T<class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.A<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>>(!!0,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.A.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>>(!!0,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.T<class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.A<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>>(!!0,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.G1_C10.B.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass1,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.B.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.T.T<class Generated1056.BaseClass0,class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.T<class Generated1056.BaseClass1,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase2.A.B<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.T<class Generated1056.BaseClass0,class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1056.Generated1056::M.IBase1.A<class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method4.MI.16643()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method5.MI.16645()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod4453()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::ClassMethod4453.16647()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod2464<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod2463()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method4.MI.16643()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method5.MI.16645()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod4453()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::ClassMethod4453.16647()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method6.16646<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method5.16644()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::Method4.16642()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod2464<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::ClassMethod2464.MI.16649<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod2463()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G3_C1528::ClassMethod2463.MI.16648()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G3_C1528`1<class Generated1056.BaseClass1> on type class Generated1056.G3_C1528`1<class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod2463()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod2464<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod2463()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2464<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod2463()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.MI.10032<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method6.MI.10036<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod2464<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod2464.10038<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod2463()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod2463.10037()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method6.10035<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method5.10034()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method4.10033()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::Method7.10031<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G2_C547::ClassMethod1342.MI.10039<System.Object>()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G2_C547`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass1>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1>::Method7<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1056.BaseClass0,class Generated1056.BaseClass1> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method4()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method5()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1056.IBase1`1<class Generated1056.BaseClass0>::Method6<object>()
		calli default string(class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1056.BaseClass0> on type class Generated1056.G1_C10`2<class Generated1056.BaseClass1,class Generated1056.BaseClass1>"
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
		call void Generated1056.Generated1056::MethodCallingTest()
		call void Generated1056.Generated1056::ConstrainedCallsTest()
		call void Generated1056.Generated1056::StructConstrainedInterfaceCallsTest()
		call void Generated1056.Generated1056::CalliTest()
		ldc.i4 100
		ret
	}
}
