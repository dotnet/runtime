// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1029.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1029.BaseClass1 
		extends Generated1029.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1029.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1029.G3_C1501`1<T0> 
		extends class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		implements class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1501::Method7.16493<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<[1]>()
		ldstr "G3_C1501::Method7.MI.16494<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4393() cil managed noinlining { 
		ldstr "G3_C1501::ClassMethod4393.16495()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4394<M0>() cil managed noinlining { 
		ldstr "G3_C1501::ClassMethod4394.16496<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C515<class Generated1029.BaseClass1,class Generated1029.BaseClass1>.ClassMethod2391'<M0>() cil managed noinlining { 
		.override method instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2391<[1]>()
		ldstr "G3_C1501::ClassMethod2391.MI.16497<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::.ctor()
		ret
	}
}
.class public abstract Generated1029.G2_C515`2<T0, T1> 
		extends class Generated1029.G1_C9`2<!T1,!T1>
		implements class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>, class Generated1029.IBase1`1<!T1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C515::Method7.9719<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C515::Method4.9720()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C515::Method5.9721()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C515::Method6.9722<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T1>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1029.IBase1`1<!T1>::Method6<[1]>()
		ldstr "G2_C515::Method6.MI.9723<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2390<M0>() cil managed noinlining { 
		ldstr "G2_C515::ClassMethod2390.9724<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2391<M0>() cil managed noinlining { 
		ldstr "G2_C515::ClassMethod2391.9725<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<T1,T1>.ClassMethod1338'<M0>() cil managed noinlining { 
		.override method instance string class Generated1029.G1_C9`2<!T1,!T1>::ClassMethod1338<[1]>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C9<T1,T1>.ClassMethod1339'<M0>() cil managed noinlining { 
		.override method instance string class Generated1029.G1_C9`2<!T1,!T1>::ClassMethod1339<[1]>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1029.G1_C9`2<!T1,!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1029.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public abstract Generated1029.G1_C9`2<T0, T1> 
		implements class Generated1029.IBase2`2<class Generated1029.BaseClass0,!T0>, class Generated1029.IBase1`1<class Generated1029.BaseClass0> 
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1029.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method4()
		ldstr "G1_C9::Method4.MI.4835()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C9::Method5.4836()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1029.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method5()
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
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1029.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method6<[1]>()
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
.class interface public abstract Generated1029.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1029.Generated1029 {
	.method static void M.BaseClass0<(Generated1029.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1029.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1029.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1029.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1501.T<T0,(class Generated1029.G3_C1501`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1501.T<T0,(class Generated1029.G3_C1501`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod4393()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::ClassMethod4394<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1501.A<(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1501.A<(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod4393()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod4394<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1501.B<(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1501.B<(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod4393()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod4394<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.T.T<T0,T1,(class Generated1029.G2_C515`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.T.T<T0,T1,(class Generated1029.G2_C515`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.A.T<T1,(class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.A.T<T1,(class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.A.A<(class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.A.A<(class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.A.B<(class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.A.B<(class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.B.T<T1,(class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.B.T<T1,(class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.B.A<(class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.B.A<(class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C515.B.B<(class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C515.B.B<(class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2390<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2391<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1029.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1029.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1029.IBase2`2<class Generated1029.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1029.IBase2`2<class Generated1029.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1029.IBase2`2<class Generated1029.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1029.IBase2`2<class Generated1029.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.T.T<T0,T1,(class Generated1029.G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.T.T<T0,T1,(class Generated1029.G1_C9`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.T<T1,(class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.T<T1,(class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.A<(class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.A<(class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.A.B<(class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.A.B<(class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.T<T1,(class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.T<T1,(class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.A<(class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.A<(class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C9.B.B<(class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C9.B.B<(class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1029.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1029.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1029.IBase1`1<class Generated1029.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1029.IBase1`1<class Generated1029.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1029.IBase1`1<class Generated1029.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1029.IBase1`1<class Generated1029.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method6<object>()
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2391<object>()
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2390<object>()
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod4394<object>()
		ldstr "G3_C1501::ClassMethod4394.16496<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod4393()
		ldstr "G3_C1501::ClassMethod4393.16495()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod2391<object>()
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod2390<object>()
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method6<object>()
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1339<object>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1338<object>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method6<object>()
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2391<object>()
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2390<object>()
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod4394<object>()
		ldstr "G3_C1501::ClassMethod4394.16496<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod4393()
		ldstr "G3_C1501::ClassMethod4393.16495()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method7<object>()
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod2391<object>()
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod2390<object>()
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method6<object>()
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method5()
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method4()
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1339<object>()
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1338<object>()
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1337()
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		callvirt instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1336()
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
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
		newobj instance void class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G1_C9::Method6.4838<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G1_C9.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G1_C9::Method6.4838<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G1_C9.B.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G1_C9::Method6.4838<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G1_C9.B.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass0,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G2_C515.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G2_C515.B.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G2_C515.B.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass0,class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G3_C1501::ClassMethod4393.16495()#G3_C1501::ClassMethod4394.16496<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G3_C1501.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G3_C1501::ClassMethod4393.16495()#G3_C1501::ClassMethod4394.16496<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G3_C1501.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>>(!!0,string)
		newobj instance void class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G1_C9::Method6.4838<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G1_C9.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G1_C9::Method6.4838<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G1_C9.B.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G1_C9::Method6.4838<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G1_C9.B.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass0,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G2_C515.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G2_C515.B.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G2_C515.B.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.MI.9723<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase1.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass0,class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.T<class Generated1029.BaseClass0,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.A.A<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.T.T<class Generated1029.BaseClass1,class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()#"
		call void Generated1029.Generated1029::M.IBase2.B.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G3_C1501::ClassMethod4393.16495()#G3_C1501::ClassMethod4394.16496<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G3_C1501.T<class Generated1029.BaseClass1,class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C9::ClassMethod1336.4840()#G1_C9::ClassMethod1337.4841()#G2_C515::ClassMethod1338.MI.9726<System.Object>()#G2_C515::ClassMethod1339.MI.9727<System.Object>()#G2_C515::ClassMethod2390.9724<System.Object>()#G3_C1501::ClassMethod2391.MI.16497<System.Object>()#G3_C1501::ClassMethod4393.16495()#G3_C1501::ClassMethod4394.16496<System.Object>()#G2_C515::Method4.9720()#G2_C515::Method5.9721()#G2_C515::Method6.9722<System.Object>()#G3_C1501::Method7.16493<System.Object>()#"
		call void Generated1029.Generated1029::M.G3_C1501.B<class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2391<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2390<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod4394<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::ClassMethod4394.16496<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod4393()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::ClassMethod4393.16495()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod2391<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod2390<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1339<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1338<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1337()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>::ClassMethod1336()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::Method6.4838<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G1_C9`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass0>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2391<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod2390<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1337()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::ClassMethod1336()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G2_C515`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase1`1<class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method6.MI.9723<System.Object>()"
		ldstr "class IBase1`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass0,class Generated1029.BaseClass0> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1029.IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.MI.16494<System.Object>()"
		ldstr "class IBase2`2<class Generated1029.BaseClass1,class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod4394<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::ClassMethod4394.16496<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod4393()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::ClassMethod4393.16495()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method7<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::Method7.16493<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod2391<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G3_C1501::ClassMethod2391.MI.16497<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod2390<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod2390.9724<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method6<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method6.9722<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method5()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method5.9721()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::Method4()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::Method4.9720()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1339<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod1339.MI.9727<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1338<object>()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G2_C515::ClassMethod1338.MI.9726<System.Object>()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1337()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::ClassMethod1337.4841()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>::ClassMethod1336()
		calli default string(class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>)
		ldstr "G1_C9::ClassMethod1336.4840()"
		ldstr "class G3_C1501`1<class Generated1029.BaseClass1> on type class Generated1029.G3_C1501`1<class Generated1029.BaseClass1>"
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
		call void Generated1029.Generated1029::MethodCallingTest()
		call void Generated1029.Generated1029::ConstrainedCallsTest()
		call void Generated1029.Generated1029::StructConstrainedInterfaceCallsTest()
		call void Generated1029.Generated1029::CalliTest()
		ldc.i4 100
		ret
	}
}
