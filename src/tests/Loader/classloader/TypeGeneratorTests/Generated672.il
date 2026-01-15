// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated672.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated672.BaseClass1 
		extends Generated672.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated672.BaseClass0::.ctor()
		ret
	}
}
.class public Generated672.G3_C1144`1<T0> 
		extends class Generated672.G2_C200`1<class Generated672.BaseClass0>
		implements class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1144::Method7.14574<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated672.BaseClass0,class Generated672.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<[1]>()
		ldstr "G3_C1144::Method7.MI.14575<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C200<class Generated672.BaseClass0>.ClassMethod1731'() cil managed noinlining { 
		.override method instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1731()
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated672.G2_C200`1<class Generated672.BaseClass0>::.ctor()
		ret
	}
}
.class public abstract Generated672.G2_C200`1<T0> 
		extends class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		implements Generated672.IBase0, class Generated672.IBase2`2<!T0,!T0> 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G2_C200::Method0.6641()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated672.IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated672.IBase0::Method0()
		ldstr "G2_C200::Method0.MI.6642()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G2_C200::Method1.6643()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated672.IBase0.Method1'() cil managed noinlining { 
		.override method instance string Generated672.IBase0::Method1()
		ldstr "G2_C200::Method1.MI.6644()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G2_C200::Method2.6645<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G2_C200::Method3.6646<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C200::Method7.6647<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T0>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated672.IBase2`2<!T0,!T0>::Method7<[1]>()
		ldstr "G2_C200::Method7.MI.6648<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1731() cil managed noinlining { 
		ldstr "G2_C200::ClassMethod1731.6649()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1732<M0>() cil managed noinlining { 
		ldstr "G2_C200::ClassMethod1732.6650<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C14<class Generated672.BaseClass1,class Generated672.BaseClass0>.ClassMethod1350'<M0>() cil managed noinlining { 
		.override method instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<[1]>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C14<class Generated672.BaseClass1,class Generated672.BaseClass0>.ClassMethod1351'<M0>() cil managed noinlining { 
		.override method instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<[1]>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated672.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated672.G1_C14`2<T0, T1> 
		implements class Generated672.IBase2`2<!T1,class Generated672.BaseClass1>, class Generated672.IBase1`1<class Generated672.BaseClass1> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C14::Method7.4878<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G1_C14::Method4.4879()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G1_C14::Method5.4880()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated672.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ret
	}
	.method public hidebysig virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G1_C14::Method6.4882<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1350<M0>() cil managed noinlining { 
		ldstr "G1_C14::ClassMethod1350.4883<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1351<M0>() cil managed noinlining { 
		ldstr "G1_C14::ClassMethod1351.4884<"
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
.class interface public abstract Generated672.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class interface public abstract Generated672.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated672.Generated672 {
	.method static void M.BaseClass0<(Generated672.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated672.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated672.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated672.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1144.T<T0,(class Generated672.G3_C1144`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1144.T<T0,(class Generated672.G3_C1144`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::ClassMethod1731()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::ClassMethod1732<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1144.A<(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1144.A<(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1731()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1144.B<(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1144.B<(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1731()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1732<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C200.T<T0,(class Generated672.G2_C200`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C200.T<T0,(class Generated672.G2_C200`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::ClassMethod1731()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::ClassMethod1732<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C200.A<(class Generated672.G2_C200`1<class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C200.A<(class Generated672.G2_C200`1<class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1731()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C200.B<(class Generated672.G2_C200`1<class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 17
		.locals init (string[] actualResults)
		ldc.i4.s    12
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C200.B<(class Generated672.G2_C200`1<class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 12
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::ClassMethod1731()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::ClassMethod1732<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G2_C200`1<class Generated672.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated672.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated672.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated672.IBase2`2<class Generated672.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated672.IBase2`2<class Generated672.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<class Generated672.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated672.IBase2`2<class Generated672.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated672.IBase2`2<class Generated672.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<class Generated672.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.T.T<T0,T1,(class Generated672.G1_C14`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.T.T<T0,T1,(class Generated672.G1_C14`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<!!T0,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<!!T0,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.A.T<T1,(class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.A.T<T1,(class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.A.A<(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.A.A<(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.A.B<(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.A.B<(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.B.T<T1,(class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.B.T<T1,(class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.B.A<(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.B.A<(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C14.B.B<(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 11
		.locals init (string[] actualResults)
		ldc.i4.s    6
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C14.B.B<(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 6
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::ClassMethod1350<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::ClassMethod1351<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated672.IBase0<(Generated672.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated672.IBase0<(Generated672.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated672.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated672.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated672.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated672.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated672.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated672.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated672.IBase1`1<class Generated672.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated672.IBase1`1<class Generated672.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated672.IBase1`1<class Generated672.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated672.IBase1`1<class Generated672.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
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
		newobj instance void class Generated672.G3_C1144`1<class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1731()
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method7<object>()
		ldstr "G2_C200::Method7.6647<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method3<object>()
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method2<object>()
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method1()
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method0()
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated672.IBase0::Method0()
		ldstr "G2_C200::Method0.MI.6642()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated672.IBase0::Method1()
		ldstr "G2_C200::Method1.MI.6644()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated672.IBase0::Method2<object>()
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated672.IBase0::Method3<object>()
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		ldstr "G2_C200::Method7.MI.6648<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method7<object>()
		ldstr "G3_C1144::Method7.14574<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1731()
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method3<object>()
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method2<object>()
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method1()
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method0()
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated672.G3_C1144`1<class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1731()
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method7<object>()
		ldstr "G2_C200::Method7.6647<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method3<object>()
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method2<object>()
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method1()
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method0()
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		callvirt instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated672.IBase0::Method0()
		ldstr "G2_C200::Method0.MI.6642()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated672.IBase0::Method1()
		ldstr "G2_C200::Method1.MI.6644()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated672.IBase0::Method2<object>()
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated672.IBase0::Method3<object>()
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		ldstr "G2_C200::Method7.MI.6648<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method7<object>()
		ldstr "G3_C1144::Method7.14574<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1732<object>()
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1731()
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method3<object>()
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method2<object>()
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method1()
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method0()
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1351<object>()
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1350<object>()
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		callvirt instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::ClassMethod1351<object>()
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::ClassMethod1350<object>()
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		callvirt instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
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
		newobj instance void class Generated672.G3_C1144`1<class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.T.T<class Generated672.BaseClass1,class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.A<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.B<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.B<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.A<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G2_C200::Method7.6647<System.Object>()#"
		call void Generated672.Generated672::M.G2_C200.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G2_C200::Method7.6647<System.Object>()#"
		call void Generated672.Generated672::M.G2_C200.A<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::Method0.MI.6642()#G2_C200::Method1.MI.6644()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#"
		call void Generated672.Generated672::M.Generated672.IBase0<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::Method7.MI.6648<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C200::Method7.MI.6648<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::Method7.MI.6648<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.A<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G3_C1144::Method7.14574<System.Object>()#"
		call void Generated672.Generated672::M.G3_C1144.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G3_C1144::Method7.14574<System.Object>()#"
		call void Generated672.Generated672::M.G3_C1144.A<class Generated672.G3_C1144`1<class Generated672.BaseClass0>>(!!0,string)
		newobj instance void class Generated672.G3_C1144`1<class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.T.T<class Generated672.BaseClass1,class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.A<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.B<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.B<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.A<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G2_C200::Method7.6647<System.Object>()#"
		call void Generated672.Generated672::M.G2_C200.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G2_C200::Method7.6647<System.Object>()#"
		call void Generated672.Generated672::M.G2_C200.A<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::Method0.MI.6642()#G2_C200::Method1.MI.6644()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#"
		call void Generated672.Generated672::M.Generated672.IBase0<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::Method7.MI.6648<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C200::Method7.MI.6648<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass0,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::Method7.MI.6648<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.A<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G3_C1144::Method7.14574<System.Object>()#"
		call void Generated672.Generated672::M.G3_C1144.T<class Generated672.BaseClass1,class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()#G2_C200::ClassMethod1351.MI.6652<System.Object>()#G3_C1144::ClassMethod1731.MI.14576()#G2_C200::ClassMethod1732.6650<System.Object>()#G2_C200::Method0.6641()#G2_C200::Method1.6643()#G2_C200::Method2.6645<System.Object>()#G2_C200::Method3.6646<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G3_C1144::Method7.14574<System.Object>()#"
		call void Generated672.Generated672::M.G3_C1144.B<class Generated672.G3_C1144`1<class Generated672.BaseClass1>>(!!0,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.T.T<class Generated672.BaseClass0,class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.A.T<class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.A.A<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.B<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.B<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.A<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>>(!!0,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.A.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.A.B<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass1,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.B.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.B.B<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.B<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.B<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.A<class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>>(!!0,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.T.T<class Generated672.BaseClass1,class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.T<class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.A<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.B<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.B<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.A<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>>(!!0,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.T.T<class Generated672.BaseClass1,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()#G1_C14::ClassMethod1351.4884<System.Object>()#G1_C14::Method4.4879()#G1_C14::Method5.4880()#G1_C14::Method6.4882<System.Object>()#G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.G1_C14.B.B<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass1,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.B.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.B.B<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.B<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.T.T<class Generated672.BaseClass0,class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.T<class Generated672.BaseClass1,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method7.4878<System.Object>()#"
		call void Generated672.Generated672::M.IBase2.A.B<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.T<class Generated672.BaseClass0,class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C14::Method4.4879()#G1_C14::Method5.MI.4881()#G1_C14::Method6.4882<System.Object>()#"
		call void Generated672.Generated672::M.IBase1.A<class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated672.G3_C1144`1<class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1731()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method7.6647<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method3<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method2<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method1()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method0()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method0()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method0.MI.6642()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method1()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method1.MI.6644()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method2<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method3<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method7.MI.6648<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G3_C1144::Method7.14574<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1731()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method3<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method2<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method1()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method0()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated672.G3_C1144`1<class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G3_C1144::Method7.MI.14575<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1732<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1731()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method7.6647<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method3<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method2<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method1()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method0()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G2_C200`1<class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G2_C200`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G2_C200`1<class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method0()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method0.MI.6642()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method1()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method1.MI.6644()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method2<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated672.IBase0::Method3<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "Generated672.IBase0 on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method7.MI.6648<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G3_C1144::Method7.14574<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1732<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1732.6650<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1731()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G3_C1144::ClassMethod1731.MI.14576()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method3<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method3.6646<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method2<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method2.6645<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method1()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method1.6643()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method0()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::Method0.6641()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1351<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1351.MI.6652<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::ClassMethod1350<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G2_C200::ClassMethod1350.MI.6651<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G3_C1144`1<class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G3_C1144`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G3_C1144`1<class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G3_C1144`1<class Generated672.BaseClass1> on type class Generated672.G3_C1144`1<class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::ClassMethod1351<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::ClassMethod1350<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass0,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1351<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::ClassMethod1350<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::ClassMethod1351<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::ClassMethod1351.4884<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::ClassMethod1350<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::ClassMethod1350.4883<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.4880()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass1,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass1>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1>::Method7<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method7.4878<System.Object>()"
		ldstr "class IBase2`2<class Generated672.BaseClass0,class Generated672.BaseClass1> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method4()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method4.4879()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method5()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method5.MI.4881()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated672.IBase1`1<class Generated672.BaseClass0>::Method6<object>()
		calli default string(class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>)
		ldstr "G1_C14::Method6.4882<System.Object>()"
		ldstr "class IBase1`1<class Generated672.BaseClass0> on type class Generated672.G1_C14`2<class Generated672.BaseClass1,class Generated672.BaseClass1>"
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
		call void Generated672.Generated672::MethodCallingTest()
		call void Generated672.Generated672::ConstrainedCallsTest()
		call void Generated672.Generated672::StructConstrainedInterfaceCallsTest()
		call void Generated672.Generated672::CalliTest()
		ldc.i4 100
		ret
	}
}
