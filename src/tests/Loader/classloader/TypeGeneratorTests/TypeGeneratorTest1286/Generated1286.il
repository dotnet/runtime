// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1286.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1286.BaseClass1 
		extends Generated1286.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1286.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1286.G3_C1758`1<T0> 
		extends class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		implements class Generated1286.IBase2`2<!T0,class Generated1286.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1758::Method7.17838<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class Generated1286.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1286.IBase2`2<!T0,class Generated1286.BaseClass1>::Method7<[1]>()
		ldstr "G3_C1758::Method7.MI.17839<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4923<M0>() cil managed noinlining { 
		ldstr "G3_C1758::ClassMethod4923.17840<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated1286.G2_C713`2<T0, T1> 
		extends class Generated1286.G1_C13`2<!T0,class Generated1286.BaseClass1>
		implements class Generated1286.IBase1`1<!T1>, class Generated1286.IBase2`2<!T0,class Generated1286.BaseClass1> 
{
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C713::Method4.11570()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1286.IBase1`1<!T1>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C713::Method5.11572()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C713::Method6.11573<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1286.IBase1`1<!T1>::Method6<[1]>()
		ldstr "G2_C713::Method6.MI.11574<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C713::Method7.11575<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2792() cil managed noinlining { 
		ldstr "G2_C713::ClassMethod2792.11576()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<T0,class Generated1286.BaseClass1>.ClassMethod1348'<M0>() cil managed noinlining { 
		.override method instance string class Generated1286.G1_C13`2<!T0,class Generated1286.BaseClass1>::ClassMethod1348<[1]>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<T0,class Generated1286.BaseClass1>.ClassMethod1349'<M0>() cil managed noinlining { 
		.override method instance string class Generated1286.G1_C13`2<!T0,class Generated1286.BaseClass1>::ClassMethod1349<[1]>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1286.G1_C13`2<!T0,class Generated1286.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1286.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated1286.G1_C13`2<T0, T1> 
		implements class Generated1286.IBase2`2<!T0,!T1>, class Generated1286.IBase1`1<!T0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method7.4871<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C13::Method4.4872()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C13::Method5.4873()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1286.IBase1`1<!T0>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C13::Method6.4875<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1348<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1348.4876<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1349<M0>() cil managed noinlining { 
		ldstr "G1_C13::ClassMethod1349.4877<"
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
.class interface public abstract Generated1286.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1286.Generated1286 {
	.method static void M.BaseClass0<(Generated1286.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1286.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1286.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1286.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1758.T<T0,(class Generated1286.G3_C1758`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1758.T<T0,(class Generated1286.G3_C1758`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::ClassMethod4923<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1758.A<(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1758.A<(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod4923<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1758.B<(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1758.B<(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod4923<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.T.T<T0,T1,(class Generated1286.G2_C713`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.T.T<T0,T1,(class Generated1286.G2_C713`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.A.T<T1,(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.A.T<T1,(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.A.A<(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.A.A<(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.A.B<(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.A.B<(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.B.T<T1,(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.B.T<T1,(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.B.A<(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.B.A<(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C713.B.B<(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C713.B.B<(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod2792()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1286.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1286.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1286.IBase2`2<class Generated1286.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1286.IBase2`2<class Generated1286.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1286.IBase2`2<class Generated1286.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1286.IBase2`2<class Generated1286.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.T.T<T0,T1,(class Generated1286.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class Generated1286.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1286.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1286.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1286.IBase1`1<class Generated1286.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1286.IBase1`1<class Generated1286.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1286.IBase1`1<class Generated1286.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1286.IBase1`1<class Generated1286.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod4923<object>()
		ldstr "G3_C1758::ClassMethod4923.17840<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method7<object>()
		ldstr "G3_C1758::Method7.17838<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod4923<object>()
		ldstr "G3_C1758::ClassMethod4923.17840<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G3_C1758::Method7.17838<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod2792()
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		callvirt instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
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
		newobj instance void class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.T<class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.B<class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.T<class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.B<class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.A<class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.T<class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.A<class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G3_C1758::ClassMethod4923.17840<System.Object>()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G3_C1758::Method7.17838<System.Object>()#"
		call void Generated1286.Generated1286::M.G3_C1758.T<class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G3_C1758::ClassMethod4923.17840<System.Object>()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G3_C1758::Method7.17838<System.Object>()#"
		call void Generated1286.Generated1286::M.G3_C1758.A<class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>>(!!0,string)
		newobj instance void class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.T<class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.B<class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.T<class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.B<class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.A<class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.T<class Generated1286.BaseClass0,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.A<class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G3_C1758::ClassMethod4923.17840<System.Object>()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G3_C1758::Method7.17838<System.Object>()#"
		call void Generated1286.Generated1286::M.G3_C1758.T<class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G3_C1758::ClassMethod4923.17840<System.Object>()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G3_C1758::Method7.17838<System.Object>()#"
		call void Generated1286.Generated1286::M.G3_C1758.B<class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.B.T<class Generated1286.BaseClass1,class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.B.B<class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>>(!!0,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.A<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.T<class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.A<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>>(!!0,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.A<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>>(!!0,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.B.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.B.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.B.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.B.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.11570()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.11570()#G1_C13::Method5.MI.4874()#G1_C13::Method6.4875<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.A<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.B.T<class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.B.A<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>>(!!0,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.B.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::Method4.11570()#G1_C13::Method5.4873()#G1_C13::Method6.4875<System.Object>()#G1_C13::Method7.4871<System.Object>()#"
		call void Generated1286.Generated1286::M.G1_C13.B.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.B.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.B.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.T.T<class Generated1286.BaseClass0,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase2.A.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.T<class Generated1286.BaseClass0,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::Method4.MI.11571()#G2_C713::Method5.11572()#G2_C713::Method6.MI.11574<System.Object>()#"
		call void Generated1286.Generated1286::M.IBase1.A<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.T.T<class Generated1286.BaseClass1,class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.B.T<class Generated1286.BaseClass1,class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()#G2_C713::ClassMethod1349.MI.11578<System.Object>()#G2_C713::ClassMethod2792.11576()#G2_C713::Method4.11570()#G2_C713::Method5.11572()#G2_C713::Method6.11573<System.Object>()#G2_C713::Method7.11575<System.Object>()#"
		call void Generated1286.Generated1286::M.G2_C713.B.B<class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod4923<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G3_C1758::ClassMethod4923.17840<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G3_C1758::Method7.17838<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod2792()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod4923<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G3_C1758::ClassMethod4923.17840<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G3_C1758::Method7.17838<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod2792()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G3_C1758`1<class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>)
		ldstr "G3_C1758::Method7.MI.17839<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G3_C1758`1<class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod2792()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod2792()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method5.MI.4874()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod2792()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method6.4875<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method5.4873()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G1_C13::Method7.4871<System.Object>()"
		ldstr "class G1_C13`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class IBase2`2<class Generated1286.BaseClass0,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.MI.11571()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1286.IBase1`1<class Generated1286.BaseClass0>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.MI.11574<System.Object>()"
		ldstr "class IBase1`1<class Generated1286.BaseClass0> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod2792()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod2792.11576()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method7<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method7.11575<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method6<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method6.11573<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method5()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method5.11572()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::Method4()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::Method4.11570()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1349.MI.11578<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>)
		ldstr "G2_C713::ClassMethod1348.MI.11577<System.Object>()"
		ldstr "class G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1> on type class Generated1286.G2_C713`2<class Generated1286.BaseClass1,class Generated1286.BaseClass1>"
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
		call void Generated1286.Generated1286::MethodCallingTest()
		call void Generated1286.Generated1286::ConstrainedCallsTest()
		call void Generated1286.Generated1286::StructConstrainedInterfaceCallsTest()
		call void Generated1286.Generated1286::CalliTest()
		ldc.i4 100
		ret
	}
}
