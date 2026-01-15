// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1308.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1308.BaseClass1 
		extends Generated1308.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1308.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1308.G3_C1780`1<T0> 
		extends class Generated1308.G2_C729`2<!T0,!T0>
		implements class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1780::Method7.17970<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4959<M0>() cil managed noinlining { 
		ldstr "G3_C1780::ClassMethod4959.17971<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C729<T0,T0>.ClassMethod2837'<M0>() cil managed noinlining { 
		.override method instance string class Generated1308.G2_C729`2<!T0,!T0>::ClassMethod2837<[1]>()
		ldstr "G3_C1780::ClassMethod2837.MI.17972<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1308.G2_C729`2<!T0,!T0>::.ctor()
		ret
	}
}
.class public Generated1308.G2_C729`2<T0, T1> 
		extends class Generated1308.G1_C13`2<!T1,class Generated1308.BaseClass1>
		implements class Generated1308.IBase2`2<!T0,class Generated1308.BaseClass0>, class Generated1308.IBase1`1<!T1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C729::Method7.11725<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,class Generated1308.BaseClass0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1308.IBase2`2<!T0,class Generated1308.BaseClass0>::Method7<[1]>()
		ldstr "G2_C729::Method7.MI.11726<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C729::Method4.11727()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1308.IBase1`1<!T1>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C729::Method5.11729()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1308.IBase1`1<!T1>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C729::Method6.11731<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2837<M0>() cil managed noinlining { 
		ldstr "G2_C729::ClassMethod2837.11732<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<T1,class Generated1308.BaseClass1>.ClassMethod1348'<M0>() cil managed noinlining { 
		.override method instance string class Generated1308.G1_C13`2<!T1,class Generated1308.BaseClass1>::ClassMethod1348<[1]>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1308.G1_C13`2<!T1,class Generated1308.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1308.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated1308.G1_C13`2<T0, T1> 
		implements class Generated1308.IBase2`2<!T0,!T1>, class Generated1308.IBase1`1<!T0> 
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
		.override method instance string class Generated1308.IBase1`1<!T0>::Method5()
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
.class interface public abstract Generated1308.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1308.Generated1308 {
	.method static void M.BaseClass0<(Generated1308.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1308.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1308.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1308.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1780.T<T0,(class Generated1308.G3_C1780`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1780.T<T0,(class Generated1308.G3_C1780`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::ClassMethod4959<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1780.A<(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1780.A<(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod4959<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1780.B<(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1780.B<(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod4959<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.T.T<T0,T1,(class Generated1308.G2_C729`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.T.T<T0,T1,(class Generated1308.G2_C729`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.A.T<T1,(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.A.T<T1,(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.A.A<(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.A.A<(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.A.B<(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.A.B<(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.B.T<T1,(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.B.T<T1,(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.B.A<(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.B.A<(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C729.B.B<(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 12
		.locals init (string[] actualResults)
		ldc.i4.s    7
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C729.B.B<(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 7
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1308.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1308.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1308.IBase2`2<class Generated1308.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1308.IBase2`2<class Generated1308.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1308.IBase2`2<class Generated1308.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1308.IBase2`2<class Generated1308.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.T.T<T0,T1,(class Generated1308.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class Generated1308.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1308.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1308.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1308.IBase1`1<class Generated1308.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1308.IBase1`1<class Generated1308.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1308.IBase1`1<class Generated1308.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1308.IBase1`1<class Generated1308.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod4959<object>()
		ldstr "G3_C1780::ClassMethod4959.17971<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod2837<object>()
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod4959<object>()
		ldstr "G3_C1780::ClassMethod4959.17971<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod2837<object>()
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		callvirt instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
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
		newobj instance void class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.A.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.A.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.A.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.A.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G3_C1780::ClassMethod4959.17971<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G3_C1780.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G3_C1780::ClassMethod4959.17971<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G3_C1780.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>>(!!0,string)
		newobj instance void class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.B.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.B.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.B.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.B.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass0,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.A<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G3_C1780::ClassMethod4959.17971<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G3_C1780.T<class Generated1308.BaseClass1,class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G3_C1780::ClassMethod2837.MI.17972<System.Object>()#G3_C1780::ClassMethod4959.17971<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G3_C1780::Method7.17970<System.Object>()#"
		call void Generated1308.Generated1308::M.G3_C1780.B<class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>>(!!0,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.A.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.A.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>>(!!0,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.B.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.B.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>>(!!0,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.B.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.B.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>>(!!0,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.B.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G1_C13.B.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method4.MI.11728()#G2_C729::Method5.MI.11730()#G2_C729::Method6.11731<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase1.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.B.T<class Generated1308.BaseClass1,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()#G1_C13::ClassMethod1349.4877<System.Object>()#G2_C729::ClassMethod2837.11732<System.Object>()#G2_C729::Method4.11727()#G2_C729::Method5.11729()#G2_C729::Method6.11731<System.Object>()#G2_C729::Method7.11725<System.Object>()#"
		call void Generated1308.Generated1308::M.G2_C729.B.B<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass1,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.B.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.T.T<class Generated1308.BaseClass0,class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.T<class Generated1308.BaseClass0,class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C729::Method7.MI.11726<System.Object>()#"
		call void Generated1308.Generated1308::M.IBase2.A.A<class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod4959<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::ClassMethod4959.17971<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod2837<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod4959<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::ClassMethod4959.17971<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::Method7.17970<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod2837<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G3_C1780::ClassMethod2837.MI.17972<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G3_C1780`1<class Generated1308.BaseClass1> on type class Generated1308.G3_C1780`1<class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod2837<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G1_C13`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.MI.11728()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.MI.11730()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase1`1<class Generated1308.BaseClass0>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class IBase1`1<class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod2837<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod2837.11732<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method6<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method6.11731<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method5()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method5.11729()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method4()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method4.11727()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.11725<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G1_C13::ClassMethod1349.4877<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::ClassMethod1348.MI.11733<System.Object>()"
		ldstr "class G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass1,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1308.IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0>::Method7<object>()
		calli default string(class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>)
		ldstr "G2_C729::Method7.MI.11726<System.Object>()"
		ldstr "class IBase2`2<class Generated1308.BaseClass0,class Generated1308.BaseClass0> on type class Generated1308.G2_C729`2<class Generated1308.BaseClass1,class Generated1308.BaseClass1>"
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
		call void Generated1308.Generated1308::MethodCallingTest()
		call void Generated1308.Generated1308::ConstrainedCallsTest()
		call void Generated1308.Generated1308::StructConstrainedInterfaceCallsTest()
		call void Generated1308.Generated1308::CalliTest()
		ldc.i4 100
		ret
	}
}
