// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated698.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated698.BaseClass1 
		extends Generated698.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated698.BaseClass0::.ctor()
		ret
	}
}
.class public Generated698.G3_C1170`1<T0> 
		extends class Generated698.G2_C237`2<!T0,class Generated698.BaseClass0>
		implements class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1170::Method7.14710<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod3740() cil managed noinlining { 
		ldstr "G3_C1170::ClassMethod3740.14711()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C237<T0,class Generated698.BaseClass0>.ClassMethod1813'() cil managed noinlining { 
		.override method instance string class Generated698.G2_C237`2<!T0,class Generated698.BaseClass0>::ClassMethod1813()
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C237<T0,class Generated698.BaseClass0>.ClassMethod1814'() cil managed noinlining { 
		.override method instance string class Generated698.G2_C237`2<!T0,class Generated698.BaseClass0>::ClassMethod1814()
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated698.G2_C237`2<!T0,class Generated698.BaseClass0>::.ctor()
		ret
	}
}
.class public Generated698.G2_C237`2<T0, T1> 
		extends class Generated698.G1_C3`1<!T1>
		implements class Generated698.IBase2`2<!T1,class Generated698.BaseClass1>, class Generated698.IBase1`1<!T0> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C237::Method7.6954<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T1,class Generated698.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated698.IBase2`2<!T1,class Generated698.BaseClass1>::Method7<[1]>()
		ldstr "G2_C237::Method7.MI.6955<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C237::Method4.6956()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated698.IBase1`1<!T0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C237::Method5.6958()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<T0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated698.IBase1`1<!T0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C237::Method6.6960<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1813() cil managed noinlining { 
		ldstr "G2_C237::ClassMethod1813.6961()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1814() cil managed noinlining { 
		ldstr "G2_C237::ClassMethod1814.6962()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1318'() cil managed noinlining { 
		.override method instance string class Generated698.G1_C3`1<!T1>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1319'<M0>() cil managed noinlining { 
		.override method instance string class Generated698.G1_C3`1<!T1>::ClassMethod1319<[1]>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C3<T1>.ClassMethod1320'<M0>() cil managed noinlining { 
		.override method instance string class Generated698.G1_C3`1<!T1>::ClassMethod1320<[1]>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated698.G1_C3`1<!T1>::.ctor()
		ret
	}
}
.class interface public abstract Generated698.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated698.G1_C3`1<T0> 
		implements class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C3::Method7.4785<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated698.BaseClass0,class Generated698.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<[1]>()
		ldstr "G1_C3::Method7.MI.4786<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1318() cil managed noinlining { 
		ldstr "G1_C3::ClassMethod1318.4787()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1319<M0>() cil managed noinlining { 
		ldstr "G1_C3::ClassMethod1319.4788<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1320<M0>() cil managed noinlining { 
		ldstr "G1_C3::ClassMethod1320.4789<"
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
.class interface public abstract Generated698.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated698.Generated698 {
	.method static void M.BaseClass0<(Generated698.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated698.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated698.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated698.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1170.T<T0,(class Generated698.G3_C1170`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1170.T<T0,(class Generated698.G3_C1170`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::ClassMethod3740()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1170.A<(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1170.A<(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod3740()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1170.B<(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1170.B<(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod3740()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.T.T<T0,T1,(class Generated698.G2_C237`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.T.T<T0,T1,(class Generated698.G2_C237`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.A.T<T1,(class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.A.T<T1,(class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.A.A<(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.A.A<(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.A.B<(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.A.B<(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.B.T<T1,(class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.B.T<T1,(class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.B.A<(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.B.A<(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C237.B.B<(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C237.B.B<(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1813()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1814()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated698.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated698.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated698.IBase2`2<class Generated698.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated698.IBase2`2<class Generated698.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<class Generated698.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated698.IBase2`2<class Generated698.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated698.IBase2`2<class Generated698.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<class Generated698.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C3.T<T0,(class Generated698.G1_C3`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C3.T<T0,(class Generated698.G1_C3`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<!!T0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<!!T0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<!!T0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C3.A<(class Generated698.G1_C3`1<class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C3.A<(class Generated698.G1_C3`1<class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C3.B<(class Generated698.G1_C3`1<class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C3.B<(class Generated698.G1_C3`1<class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated698.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated698.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated698.IBase1`1<class Generated698.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated698.IBase1`1<class Generated698.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated698.IBase1`1<class Generated698.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated698.IBase1`1<class Generated698.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated698.G3_C1170`1<class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1814()
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1813()
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod3740()
		ldstr "G3_C1170::ClassMethod3740.14711()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1814()
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1813()
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G3_C1170`1<class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1814()
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1813()
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod3740()
		ldstr "G3_C1170::ClassMethod3740.14711()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1814()
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1813()
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1814()
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1813()
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method7<object>()
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1814()
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1813()
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1814()
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1813()
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method7<object>()
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1814()
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1813()
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method5()
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method4()
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1320<object>()
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1319<object>()
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		callvirt instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1318()
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G1_C3`1<class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated698.G1_C3`1<class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		callvirt instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
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
		newobj instance void class Generated698.G3_C1170`1<class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.A<class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.T.T<class Generated698.BaseClass0,class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.A.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.A.A<class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.A<class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G3_C1170::ClassMethod3740.14711()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G3_C1170.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G3_C1170::ClassMethod3740.14711()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G3_C1170.A<class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass1,class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.T<class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.B<class Generated698.G3_C1170`1<class Generated698.BaseClass0>>(!!0,string)
		newobj instance void class Generated698.G3_C1170`1<class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.A<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.T.T<class Generated698.BaseClass1,class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.B.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.B.A<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.B<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass0,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.A<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G3_C1170::ClassMethod3740.14711()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G3_C1170.T<class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G3_C1170::ClassMethod1813.MI.14712()#G3_C1170::ClassMethod1814.MI.14713()#G3_C1170::ClassMethod3740.14711()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.G3_C1170.B<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass1,class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.T<class Generated698.BaseClass1,class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1170::Method7.14710<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.B<class Generated698.G3_C1170`1<class Generated698.BaseClass1>>(!!0,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.A<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.T.T<class Generated698.BaseClass0,class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.A.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.A.A<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.A<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>>(!!0,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.B<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.A.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.A.B<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass1,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.B<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.A<class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>>(!!0,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.A<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.T.T<class Generated698.BaseClass1,class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.B.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.B.A<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.A<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>>(!!0,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.T.T<class Generated698.BaseClass1,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.B.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::ClassMethod1318.MI.6963()#G2_C237::ClassMethod1319.MI.6964<System.Object>()#G2_C237::ClassMethod1320.MI.6965<System.Object>()#G2_C237::ClassMethod1813.6961()#G2_C237::ClassMethod1814.6962()#G2_C237::Method4.6956()#G2_C237::Method5.6958()#G2_C237::Method6.6960<System.Object>()#G2_C237::Method7.6954<System.Object>()#"
		call void Generated698.Generated698::M.G2_C237.B.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass1,class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method7.MI.6955<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.B.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass1,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.B<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.T<class Generated698.BaseClass0,class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C237::Method4.MI.6957()#G2_C237::Method5.MI.6959()#G2_C237::Method6.6960<System.Object>()#"
		call void Generated698.Generated698::M.IBase1.A<class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>>(!!0,string)
		newobj instance void class Generated698.G1_C3`1<class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass0,class Generated698.G1_C3`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.A<class Generated698.G1_C3`1<class Generated698.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G1_C3`1<class Generated698.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G1_C3`1<class Generated698.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G1_C3`1<class Generated698.BaseClass0>>(!!0,string)
		newobj instance void class Generated698.G1_C3`1<class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.T<class Generated698.BaseClass1,class Generated698.G1_C3`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::ClassMethod1318.4787()#G1_C3::ClassMethod1319.4788<System.Object>()#G1_C3::ClassMethod1320.4789<System.Object>()#G1_C3::Method7.4785<System.Object>()#"
		call void Generated698.Generated698::M.G1_C3.B<class Generated698.G1_C3`1<class Generated698.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.T.T<class Generated698.BaseClass0,class Generated698.BaseClass1,class Generated698.G1_C3`1<class Generated698.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.T<class Generated698.BaseClass1,class Generated698.G1_C3`1<class Generated698.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C3::Method7.MI.4786<System.Object>()#"
		call void Generated698.Generated698::M.IBase2.A.B<class Generated698.G1_C3`1<class Generated698.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated698.G3_C1170`1<class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1814()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1813()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod3740()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::ClassMethod3740.14711()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1814()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1813()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass0>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G3_C1170`1<class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1814()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1813()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod3740()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::ClassMethod3740.14711()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1814()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::ClassMethod1814.MI.14713()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1813()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::ClassMethod1813.MI.14712()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method6<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method5()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::Method4()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G3_C1170`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G3_C1170`1<class Generated698.BaseClass1>::ClassMethod1318()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G3_C1170`1<class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G3_C1170`1<class Generated698.BaseClass1>)
		ldstr "G3_C1170::Method7.14710<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G3_C1170`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1814()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1813()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1814()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1813()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass0,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1814()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1813()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1814()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1814.6962()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1813()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1813.6961()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.6958()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.6956()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method7.6954<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1320<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1320.MI.6965<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1319<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1319.MI.6964<System.Object>()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::ClassMethod1318()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::ClassMethod1318.MI.6963()"
		ldstr "class G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method7.MI.6955<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass1,class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass1>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass1> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method4()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method4.MI.6957()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method5()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method5.MI.6959()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase1`1<class Generated698.BaseClass0>::Method6<object>()
		calli default string(class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>)
		ldstr "G2_C237::Method6.6960<System.Object>()"
		ldstr "class IBase1`1<class Generated698.BaseClass0> on type class Generated698.G2_C237`2<class Generated698.BaseClass1,class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G1_C3`1<class Generated698.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1320<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass0>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1319<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass0>)
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::ClassMethod1318()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass0>)
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass0>::Method7<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass0>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass0> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass0>)
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated698.G1_C3`1<class Generated698.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1320<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass1>)
		ldstr "G1_C3::ClassMethod1320.4789<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1319<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass1>)
		ldstr "G1_C3::ClassMethod1319.4788<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::ClassMethod1318()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass1>)
		ldstr "G1_C3::ClassMethod1318.4787()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated698.G1_C3`1<class Generated698.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated698.G1_C3`1<class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass1>)
		ldstr "G1_C3::Method7.4785<System.Object>()"
		ldstr "class G1_C3`1<class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated698.IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1>::Method7<object>()
		calli default string(class Generated698.G1_C3`1<class Generated698.BaseClass1>)
		ldstr "G1_C3::Method7.MI.4786<System.Object>()"
		ldstr "class IBase2`2<class Generated698.BaseClass0,class Generated698.BaseClass1> on type class Generated698.G1_C3`1<class Generated698.BaseClass1>"
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
		call void Generated698.Generated698::MethodCallingTest()
		call void Generated698.Generated698::ConstrainedCallsTest()
		call void Generated698.Generated698::StructConstrainedInterfaceCallsTest()
		call void Generated698.Generated698::CalliTest()
		ldc.i4 100
		ret
	}
}
