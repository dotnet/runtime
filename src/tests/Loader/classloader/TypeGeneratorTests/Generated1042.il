// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.class public Generated1042.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated1042.BaseClass1 
		extends Generated1042.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated1042.BaseClass0::.ctor()
		ret
	}
}
.class public Generated1042.G3_C1514`1<T0> 
		extends class Generated1042.G2_C533`2<!T0,class Generated1042.BaseClass1>
		implements Generated1042.IBase0 
{
	.method public hidebysig virtual instance string Method0() cil managed noinlining { 
		ldstr "G3_C1514::Method0.16562()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'Generated1042.IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated1042.IBase0::Method0()
		ldstr "G3_C1514::Method0.MI.16563()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G3_C1514::Method1.16564()"
		ret
	}
	.method public hidebysig virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G3_C1514::Method2.16565<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G3_C1514::Method3.16566<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C533<T0,class Generated1042.BaseClass1>.ClassMethod2432'<M0>() cil managed noinlining { 
		.override method instance string class Generated1042.G2_C533`2<!T0,class Generated1042.BaseClass1>::ClassMethod2432<[1]>()
		ldstr "G3_C1514::ClassMethod2432.MI.16567<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1042.G2_C533`2<!T0,class Generated1042.BaseClass1>::.ctor()
		ret
	}
}
.class public abstract Generated1042.G2_C533`2<T0, T1> 
		extends class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!T0>
		implements class Generated1042.IBase1`1<class Generated1042.BaseClass1>, class Generated1042.IBase2`2<!T0,class Generated1042.BaseClass0> 
{
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C533::Method4.9891()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C533::Method5.9892()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated1042.BaseClass1>.Method5'() cil managed noinlining { 
		.override method instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		ldstr "G2_C533::Method5.MI.9893()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C533::Method6.9894<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C533::Method7.9895<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2431() cil managed noinlining { 
		ldstr "G2_C533::ClassMethod2431.9896()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2432<M0>() cil managed noinlining { 
		ldstr "G2_C533::ClassMethod2432.9897<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class Generated1042.BaseClass1,T0>.ClassMethod1340'() cil managed noinlining { 
		.override method instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!T0>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C10<class Generated1042.BaseClass1,T0>.ClassMethod1342'<M0>() cil managed noinlining { 
		.override method instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!T0>::ClassMethod1342<[1]>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!T0>::.ctor()
		ret
	}
}
.class interface public abstract Generated1042.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public Generated1042.G1_C10`2<T0, T1> 
		implements class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>, class Generated1042.IBase1`1<!T0> 
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
		.override method instance string class Generated1042.IBase1`1<!T0>::Method4()
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
		.override method instance string class Generated1042.IBase1`1<!T0>::Method6<[1]>()
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
.class interface public abstract Generated1042.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated1042.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated1042.Generated1042 {
	.method static void M.BaseClass0<(Generated1042.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated1042.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated1042.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated1042.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1514.T<T0,(class Generated1042.G3_C1514`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1514.T<T0,(class Generated1042.G3_C1514`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1514.A<(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1514.A<(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1514.B<(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 19
		.locals init (string[] actualResults)
		ldc.i4.s    14
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1514.B<(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 14
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.T.T<T0,T1,(class Generated1042.G2_C533`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.T.T<T0,T1,(class Generated1042.G2_C533`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.A.T<T1,(class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.A.T<T1,(class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.A.A<(class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.A.A<(class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.A.B<(class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.A.B<(class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.B.T<T1,(class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.B.T<T1,(class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.B.A<(class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.B.A<(class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C533.B.B<(class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 15
		.locals init (string[] actualResults)
		ldc.i4.s    10
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C533.B.B<(class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 10
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod2431()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod2432<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.Generated1042.IBase0<(Generated1042.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.Generated1042.IBase0<(Generated1042.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1042.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1042.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1042.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated1042.IBase0::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.T.T<T0,T1,(class Generated1042.G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.T.T<T0,T1,(class Generated1042.G1_C10`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.T<T1,(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.T<T1,(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.A<(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.A<(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.A.B<(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.A.B<(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.T<T1,(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.T<T1,(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.A<(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.A<(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C10.B.B<(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 13
		.locals init (string[] actualResults)
		ldc.i4.s    8
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C10.B.B<(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 8
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated1042.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated1042.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated1042.IBase1`1<class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated1042.IBase1`1<class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated1042.IBase1`1<class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated1042.IBase1`1<class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated1042.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated1042.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated1042.IBase2`2<class Generated1042.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated1042.IBase2`2<class Generated1042.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated1042.IBase2`2<class Generated1042.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated1042.IBase2`2<class Generated1042.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
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
		newobj instance void class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod2432<object>()
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod2431()
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method5()
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method3<object>()
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method2<object>()
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method1()
		ldstr "G3_C1514::Method1.16564()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method0()
		ldstr "G3_C1514::Method0.16562()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod2432<object>()
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod2431()
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1342<object>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1042.IBase0::Method0()
		ldstr "G3_C1514::Method0.MI.16563()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1042.IBase0::Method1()
		ldstr "G3_C1514::Method1.16564()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1042.IBase0::Method2<object>()
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1042.IBase0::Method3<object>()
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod2432<object>()
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod2431()
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method3<object>()
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method2<object>()
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method1()
		ldstr "G3_C1514::Method1.16564()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method0()
		ldstr "G3_C1514::Method0.16562()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod2432<object>()
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod2431()
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method5()
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method4()
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1342<object>()
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1340()
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated1042.IBase0::Method0()
		ldstr "G3_C1514::Method0.MI.16563()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1042.IBase0::Method1()
		ldstr "G3_C1514::Method1.16564()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1042.IBase0::Method2<object>()
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated1042.IBase0::Method3<object>()
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		callvirt instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
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
		newobj instance void class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G2_C533.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G2_C533.A.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G2_C533.A.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G3_C1514::Method0.16562()#G3_C1514::Method1.16564()#G3_C1514::Method2.16565<System.Object>()#G3_C1514::Method3.16566<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G3_C1514.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G3_C1514::Method0.16562()#G3_C1514::Method1.16564()#G3_C1514::Method2.16565<System.Object>()#G3_C1514::Method3.16566<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G3_C1514.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1514::Method0.MI.16563()#G3_C1514::Method1.16564()#G3_C1514::Method2.16565<System.Object>()#G3_C1514::Method3.16566<System.Object>()#"
		call void Generated1042.Generated1042::M.Generated1042.IBase0<class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>>(!!0,string)
		newobj instance void class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method4.9891()#G2_C533::Method5.MI.9893()#G2_C533::Method6.9894<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G2_C533.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G2_C533.B.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G2_C533.B.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass0,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.A<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G3_C1514::Method0.16562()#G3_C1514::Method1.16564()#G3_C1514::Method2.16565<System.Object>()#G3_C1514::Method3.16566<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G3_C1514.T<class Generated1042.BaseClass1,class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C533::ClassMethod1340.MI.9898()#G1_C10::ClassMethod1341.4851()#G2_C533::ClassMethod1342.MI.9899<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G2_C533::ClassMethod2431.9896()#G3_C1514::ClassMethod2432.MI.16567<System.Object>()#G3_C1514::Method0.16562()#G3_C1514::Method1.16564()#G3_C1514::Method2.16565<System.Object>()#G3_C1514::Method3.16566<System.Object>()#G2_C533::Method4.9891()#G2_C533::Method5.9892()#G2_C533::Method6.9894<System.Object>()#G2_C533::Method7.9895<System.Object>()#"
		call void Generated1042.Generated1042::M.G3_C1514.B<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1514::Method0.MI.16563()#G3_C1514::Method1.16564()#G3_C1514::Method2.16565<System.Object>()#G3_C1514::Method3.16566<System.Object>()#"
		call void Generated1042.Generated1042::M.Generated1042.IBase0<class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>>(!!0,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.A.T<class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.A.A<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.A<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>>(!!0,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.A.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.A.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.A<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>>(!!0,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.T<class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.A<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.A<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>>(!!0,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::ClassMethod1340.4850()#G1_C10::ClassMethod1341.4851()#G1_C10::ClassMethod1342.4852<System.Object>()#G1_C10::ClassMethod1343.4853<System.Object>()#G1_C10::Method4.4845()#G1_C10::Method5.4847()#G1_C10::Method6.4848<System.Object>()#G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.G1_C10.B.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass1,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.B.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.T.T<class Generated1042.BaseClass0,class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.T<class Generated1042.BaseClass1,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method7.4844<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase2.A.B<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.T<class Generated1042.BaseClass0,class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C10::Method4.MI.4846()#G1_C10::Method5.4847()#G1_C10::Method6.MI.4849<System.Object>()#"
		call void Generated1042.Generated1042::M.IBase1.A<class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod2432<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod2431()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method3<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method2<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method1()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method1.16564()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method0()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method0.16562()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod2432<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod2431()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method0()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method0.MI.16563()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method1()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method1.16564()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method2<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method3<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>)
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method5.MI.9893()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod2432<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod2431()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G2_C533`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method3<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method2<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method1()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method1.16564()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method0()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method0.16562()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod2432<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::ClassMethod2432.MI.16567<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod2431()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod2431.9896()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method7.9895<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method6.9894<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method5.9892()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::Method4.9891()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod1342.MI.9899<System.Object>()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G2_C533::ClassMethod1340.MI.9898()"
		ldstr "class G3_C1514`1<class Generated1042.BaseClass1> on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method0()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method0.MI.16563()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method1()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method1.16564()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method2<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method2.16565<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated1042.IBase0::Method3<object>()
		calli default string(class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>)
		ldstr "G3_C1514::Method3.16566<System.Object>()"
		ldstr "Generated1042.IBase0 on type class Generated1042.G3_C1514`1<class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1343<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1342<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1341()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::ClassMethod1340()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1343<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1343.4853<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1342<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1342.4852<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1341()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1341.4851()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::ClassMethod1340()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::ClassMethod1340.4850()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method6.4848<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method4.4845()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass1>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1>::Method7<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method7.4844<System.Object>()"
		ldstr "class IBase2`2<class Generated1042.BaseClass0,class Generated1042.BaseClass1> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method4()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method4.MI.4846()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method5()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method5.4847()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated1042.IBase1`1<class Generated1042.BaseClass0>::Method6<object>()
		calli default string(class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>)
		ldstr "G1_C10::Method6.MI.4849<System.Object>()"
		ldstr "class IBase1`1<class Generated1042.BaseClass0> on type class Generated1042.G1_C10`2<class Generated1042.BaseClass1,class Generated1042.BaseClass1>"
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
		call void Generated1042.Generated1042::MethodCallingTest()
		call void Generated1042.Generated1042::ConstrainedCallsTest()
		call void Generated1042.Generated1042::StructConstrainedInterfaceCallsTest()
		call void Generated1042.Generated1042::CalliTest()
		ldc.i4 100
		ret
	}
}
