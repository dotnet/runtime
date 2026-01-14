// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated1251.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1251.BaseClass1 
		extends Generated1251.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1251.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1251.G3_C1723`1<T0> 
		extends class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		implements class Generated1251.IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G3_C1723::Method4.17647()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated1251.IBase1`1<!T0>::Method4()
		ldstr "G3_C1723::Method4.MI.17648()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G3_C1723::Method5.17649()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1251.IBase1`1<!T0>::Method5()
		ldstr "G3_C1723::Method5.MI.17650()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G3_C1723::Method6.17651<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C695<class Generated1251.BaseClass1,class Generated1251.BaseClass0>.ClassMethod2754'<M0>() cil managed noinlining { 
		.override method instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<[1]>()
		ldstr "G3_C1723::ClassMethod2754.MI.17652<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C695<class Generated1251.BaseClass1,class Generated1251.BaseClass0>.ClassMethod2755'<M0>() cil managed noinlining { 
		.override method instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<[1]>()
		ldstr "G3_C1723::ClassMethod2755.MI.17653<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated1251.G2_C695`2<T0, T1> 
		extends class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		implements class Generated1251.IBase2`2<!T0,!T1>, class Generated1251.IBase1`1<class Generated1251.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C695::Method7.11403<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated1251.IBase2`2<!T0,!T1>::Method7<[1]>()
		ldstr "G2_C695::Method7.MI.11404<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C695::Method4.11405()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C695::Method5.11406()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C695::Method6.11407<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1251.BaseClass0>.Method6'<M0>() cil managed noinlining { 
		.override method instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<[1]>()
		ldstr "G2_C695::Method6.MI.11408<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2753() cil managed noinlining { 
		ldstr "G2_C695::ClassMethod2753.11409()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2754<M0>() cil managed noinlining { 
		ldstr "G2_C695::ClassMethod2754.11410<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2755<M0>() cil managed noinlining { 
		ldstr "G2_C695::ClassMethod2755.11411<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<class Generated1251.BaseClass0,class Generated1251.BaseClass1>.ClassMethod1348'<M0>() cil managed noinlining { 
		.override method instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<[1]>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C13<class Generated1251.BaseClass0,class Generated1251.BaseClass1>.ClassMethod1349'<M0>() cil managed noinlining { 
		.override method instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<[1]>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::.ctor()
		ret
	}
}
.class interface public abstract Generated1251.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public abstract Generated1251.G1_C13`2<T0, T1> 
		implements class Generated1251.IBase2`2<!T0,!T1>, class Generated1251.IBase1`1<!T0> 
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
		.override method instance string class Generated1251.IBase1`1<!T0>::Method5()
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
.class interface public abstract Generated1251.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1251.Generated1251 {
	.method static void M.BaseClass0<(Generated1251.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1251.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1251.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1251.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1723.T<T0,(class Generated1251.G3_C1723`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1723.T<T0,(class Generated1251.G3_C1723`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1723.A<(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1723.A<(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1723.B<(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1723.B<(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.T.T<T0,T1,(class Generated1251.G2_C695`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.T.T<T0,T1,(class Generated1251.G2_C695`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.A.T<T1,(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.A.T<T1,(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.A.A<(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.A.A<(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.A.B<(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.A.B<(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.B.T<T1,(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.B.T<T1,(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.B.A<(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.B.A<(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C695.B.B<(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C695.B.B<(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2753()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2754<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2755<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1251.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1251.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1251.IBase1`1<class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1251.IBase1`1<class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1251.IBase1`1<class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1251.IBase1`1<class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.T.T<T0,T1,(class Generated1251.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.T.T<T0,T1,(class Generated1251.G1_C13`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<!!T0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<!!T0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.T<T1,(class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.T<T1,(class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.A<(class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.A<(class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.A.B<(class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.A.B<(class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.T<T1,(class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.T<T1,(class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.A<(class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.A<(class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C13.B.B<(class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C13.B.B<(class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1251.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1251.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1251.IBase2`2<class Generated1251.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1251.IBase2`2<class Generated1251.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1251.IBase2`2<class Generated1251.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1251.IBase2`2<class Generated1251.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G3_C1723::Method4.MI.17648()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G3_C1723::Method5.MI.17650()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G3_C1723::Method5.17649()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G3_C1723::Method4.17647()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2755<object>()
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2754<object>()
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G3_C1723::Method4.MI.17648()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G3_C1723::Method5.MI.17650()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method5()
		ldstr "G3_C1723::Method5.17649()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method4()
		ldstr "G3_C1723::Method4.17647()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2755<object>()
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2754<object>()
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method4()
		ldstr "G3_C1723::Method4.MI.17648()"
		ldstr "class IBase1`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method5()
		ldstr "G3_C1723::Method5.MI.17650()"
		ldstr "class IBase1`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2755<object>()
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2754<object>()
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2755<object>()
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2754<object>()
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2753()
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method6<object>()
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method5()
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method4()
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		callvirt instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
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
		newobj instance void class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1723::Method4.MI.17648()#G3_C1723::Method5.MI.17650()#G3_C1723::Method6.17651<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1723::Method4.MI.17648()#G3_C1723::Method5.MI.17650()#G3_C1723::Method6.17651<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G3_C1723::Method4.17647()#G3_C1723::Method5.17649()#G3_C1723::Method6.17651<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G3_C1723.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G3_C1723::Method4.17647()#G3_C1723::Method5.17649()#G3_C1723::Method6.17651<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G3_C1723.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>>(!!0,string)
		newobj instance void class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1723::Method4.MI.17648()#G3_C1723::Method5.MI.17650()#G3_C1723::Method6.17651<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1723::Method4.MI.17648()#G3_C1723::Method5.MI.17650()#G3_C1723::Method6.17651<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass0,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.A<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G3_C1723::Method4.17647()#G3_C1723::Method5.17649()#G3_C1723::Method6.17651<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G3_C1723.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G3_C1723::ClassMethod2754.MI.17652<System.Object>()#G3_C1723::ClassMethod2755.MI.17653<System.Object>()#G3_C1723::Method4.17647()#G3_C1723::Method5.17649()#G3_C1723::Method6.17651<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G3_C1723.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1723::Method4.MI.17648()#G3_C1723::Method5.MI.17650()#G3_C1723::Method6.17651<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass1,class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1723::Method4.MI.17648()#G3_C1723::Method5.MI.17650()#G3_C1723::Method6.17651<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.B<class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>>(!!0,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.A.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.A.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>>(!!0,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>>(!!0,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>>(!!0,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G1_C13::Method4.4872()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G1_C13.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass0,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.A.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.T<class Generated1251.BaseClass0,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.MI.11408<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase1.A<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()#G2_C695::ClassMethod1349.MI.11413<System.Object>()#G2_C695::ClassMethod2753.11409()#G2_C695::ClassMethod2754.11410<System.Object>()#G2_C695::ClassMethod2755.11411<System.Object>()#G2_C695::Method4.11405()#G2_C695::Method5.11406()#G2_C695::Method6.11407<System.Object>()#G2_C695::Method7.11403<System.Object>()#"
		call void Generated1251.Generated1251::M.G2_C695.B.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.T.T<class Generated1251.BaseClass1,class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.T<class Generated1251.BaseClass1,class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C695::Method7.MI.11404<System.Object>()#"
		call void Generated1251.Generated1251::M.IBase2.B.B<class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::Method4.MI.17648()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::Method5.MI.17650()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::Method5.17649()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::Method4.17647()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2755<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2754<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod2753()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method4.MI.17648()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method5.MI.17650()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method5.17649()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method4.17647()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2755<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::ClassMethod2755.MI.17653<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2754<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::ClassMethod2754.MI.17652<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod2753()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G3_C1723`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method4.MI.17648()"
		ldstr "class IBase1`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method5.MI.17650()"
		ldstr "class IBase1`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>)
		ldstr "G3_C1723::Method6.17651<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass1> on type class Generated1251.G3_C1723`1<class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod2753()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2755<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2754<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod2753()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2755<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2754<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod2753()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G1_C13::Method4.4872()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G1_C13`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass0,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase1`1<class Generated1251.BaseClass0>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.MI.11408<System.Object>()"
		ldstr "class IBase1`1<class Generated1251.BaseClass0> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2755<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2755.11411<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2754<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2754.11410<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod2753()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod2753.11409()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method6<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method6.11407<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method5()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method5.11406()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method4()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method4.11405()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.11403<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1349<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1349.MI.11413<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::ClassMethod1348<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::ClassMethod1348.MI.11412<System.Object>()"
		ldstr "class G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1251.IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>::Method7<object>()
		calli default string(class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>)
		ldstr "G2_C695::Method7.MI.11404<System.Object>()"
		ldstr "class IBase2`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1> on type class Generated1251.G2_C695`2<class Generated1251.BaseClass1,class Generated1251.BaseClass1>"
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
		call void Generated1251.Generated1251::MethodCallingTest()
		call void Generated1251.Generated1251::ConstrainedCallsTest()
		call void Generated1251.Generated1251::StructConstrainedInterfaceCallsTest()
		call void Generated1251.Generated1251::CalliTest()
		ldc.i4 100
		ret
	}
}
