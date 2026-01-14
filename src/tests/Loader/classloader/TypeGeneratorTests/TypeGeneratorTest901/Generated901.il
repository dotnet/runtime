// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) .ver 4:0:0:0 }
.assembly extern TestFramework { .publickeytoken = ( B0 3F 5F 7F 11 D5 0A 3A )  }

//TYPES IN FORWARDER ASSEMBLIES:

//TEST ASSEMBLY:
.assembly extern xunit.core {}

.class public Generated901.BaseClass0 
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class public Generated901.BaseClass1 
		extends Generated901.BaseClass0
{
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Generated901.BaseClass0::.ctor()
		ret
	}
}
.class public Generated901.G3_C1373`1<T0> 
		extends class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		implements class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> 
{
	.method public hidebysig newslot virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G3_C1373::Method7.15823<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4133() cil managed noinlining { 
		ldstr "G3_C1373::ClassMethod4133.15824()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod4134<M0>() cil managed noinlining { 
		ldstr "G3_C1373::ClassMethod4134.15825<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'G2_C400<class Generated901.BaseClass1,class Generated901.BaseClass0>.ClassMethod2150'() cil managed noinlining { 
		.override method instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod2150()
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::.ctor()
		ret
	}
}
.class public abstract Generated901.G2_C400`2<T0, T1> 
		extends class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		implements class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>, class Generated901.IBase1`1<class Generated901.BaseClass0> 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G2_C400::Method7.8555<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<class Generated901.BaseClass1,class Generated901.BaseClass1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<[1]>()
		ldstr "G2_C400::Method7.MI.8556<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method4() cil managed noinlining { 
		ldstr "G2_C400::Method4.8557()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated901.BaseClass0>.Method4'() cil managed noinlining { 
		.override method instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method4()
		ldstr "G2_C400::Method4.MI.8558()"
		ret
	}
	.method public hidebysig virtual instance string Method5() cil managed noinlining { 
		ldstr "G2_C400::Method5.8559()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase1<class Generated901.BaseClass0>.Method5'() cil managed noinlining { 
		.override method instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method5()
		ldstr "G2_C400::Method5.MI.8560()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method6<M0>() cil managed noinlining { 
		ldstr "G2_C400::Method6.8561<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod2150() cil managed noinlining { 
		ldstr "G2_C400::ClassMethod2150.8562()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class Generated901.BaseClass0,class Generated901.BaseClass0>.ClassMethod1333'() cil managed noinlining { 
		.override method instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'G1_C8<class Generated901.BaseClass0,class Generated901.BaseClass0>.ClassMethod1335'<M0>() cil managed noinlining { 
		.override method instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<[1]>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::.ctor()
		ret
	}
}
.class interface public abstract Generated901.IBase2`2<+T0, -T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method7<M0>() cil managed { }
}
.class public Generated901.G1_C8`2<T0, T1> 
		implements class Generated901.IBase2`2<!T0,!T1>, IBase0 
{
	.method public hidebysig virtual instance string Method7<M0>() cil managed noinlining { 
		ldstr "G1_C8::Method7.4821<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase2<T0,T1>.Method7'<M0>() cil managed noinlining { 
		.override method instance string class Generated901.IBase2`2<!T0,!T1>::Method7<[1]>()
		ldstr "G1_C8::Method7.MI.4822<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method0() cil managed noinlining { 
		ldstr "G1_C8::Method0.4823()"
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method0'() cil managed noinlining { 
		.override method instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ret
	}
	.method public hidebysig virtual instance string Method1() cil managed noinlining { 
		ldstr "G1_C8::Method1.4825()"
		ret
	}
	.method public hidebysig newslot virtual instance string Method2<M0>() cil managed noinlining { 
		ldstr "G1_C8::Method2.4826<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string 'IBase0.Method2'<M0>() cil managed noinlining { 
		.override method instance string Generated901.IBase0::Method2<[1]>()
		ldstr "G1_C8::Method2.MI.4827<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string Method3<M0>() cil managed noinlining { 
		ldstr "G1_C8::Method3.4828<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1332() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1332.4829()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1333() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1333.4830()"
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1334<M0>() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1334.4831<"
		ldtoken !!M0
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		call string [mscorlib]System.String::Concat(object,object)
		ldstr ">()"
		call string [mscorlib]System.String::Concat(object,object)
		ret
	}
	.method public hidebysig newslot virtual instance string ClassMethod1335<M0>() cil managed noinlining { 
		ldstr "G1_C8::ClassMethod1335.4832<"
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
.class interface public abstract Generated901.IBase1`1<+T0> 
{
	.method public hidebysig newslot abstract virtual instance string Method4() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method5() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method6<M0>() cil managed { }
}
.class interface public abstract Generated901.IBase0 
{
	.method public hidebysig newslot abstract virtual instance string Method0() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method1() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method2<M0>() cil managed { }
	.method public hidebysig newslot abstract virtual instance string Method3<M0>() cil managed { }
}
.class public auto ansi beforefieldinit Generated901.Generated901 {
	.method static void M.BaseClass0<(Generated901.BaseClass0)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass0<(Generated901.BaseClass0)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.BaseClass1<(Generated901.BaseClass1)W>(!!W inst, string exp) cil managed {
		.maxstack 5
		.locals init (string[] actualResults)
		ldc.i4.s    0
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.BaseClass1<(Generated901.BaseClass1)W>(!!W inst, string exp)"
		ldc.i4.s 0
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1373.T<T0,(class Generated901.G3_C1373`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1373.T<T0,(class Generated901.G3_C1373`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod4133()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::ClassMethod4134<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<!!T0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1373.A<(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1373.A<(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod4133()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod4134<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G3_C1373.B<(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 20
		.locals init (string[] actualResults)
		ldc.i4.s    15
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G3_C1373.B<(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 15
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod4133()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod4134<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     13
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     14
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.T.T<T0,T1,(class Generated901.G2_C400`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.T.T<T0,T1,(class Generated901.G2_C400`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.A.T<T1,(class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.A.T<T1,(class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.A.A<(class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.A.A<(class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.A.B<(class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.A.B<(class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.B.T<T1,(class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.B.T<T1,(class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.B.A<(class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.B.A<(class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G2_C400.B.B<(class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 18
		.locals init (string[] actualResults)
		ldc.i4.s    13
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G2_C400.B.B<(class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 13
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod2150()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     9
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     10
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     11
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     12
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.T.T<T0,T1,(class Generated901.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.T.T<T0,T1,(class Generated901.IBase2`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.T<T1,(class Generated901.IBase2`2<class Generated901.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.T<T1,(class Generated901.IBase2`2<class Generated901.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<class Generated901.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.A<(class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.A<(class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.A.B<(class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.A.B<(class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.T<T1,(class Generated901.IBase2`2<class Generated901.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.T<T1,(class Generated901.IBase2`2<class Generated901.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<class Generated901.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.A<(class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.A<(class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase2.B.B<(class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 6
		.locals init (string[] actualResults)
		ldc.i4.s    1
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase2.B.B<(class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 1
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.T.T<T0,T1,(class Generated901.G1_C8`2<!!T0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.T.T<T0,T1,(class Generated901.G1_C8`2<!!T0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<!!T0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.A.T<T1,(class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.A.T<T1,(class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.A.A<(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.A.A<(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.A.B<(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.A.B<(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.B.T<T1,(class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.B.T<T1,(class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,!!T1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.B.A<(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.B.A<(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.G1_C8.B.B<(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 14
		.locals init (string[] actualResults)
		ldc.i4.s    9
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.G1_C8.B.B<(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 9
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1332()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1333()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1334<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1335<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     4
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     5
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     6
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     7
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method3<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     8
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.T<T0,(class Generated901.IBase1`1<!!T0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.T<T0,(class Generated901.IBase1`1<!!T0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<!!T0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<!!T0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<!!T0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.A<(class Generated901.IBase1`1<class Generated901.BaseClass0>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.A<(class Generated901.IBase1`1<class Generated901.BaseClass0>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase1.B<(class Generated901.IBase1`1<class Generated901.BaseClass1>)W>(!!W 'inst', string exp) cil managed {
		.maxstack 8
		.locals init (string[] actualResults)
		ldc.i4.s    3
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase1.B<(class Generated901.IBase1`1<class Generated901.BaseClass1>)W>(!!W 'inst', string exp)"
		ldc.i4.s 3
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<class Generated901.BaseClass1>::Method4()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<class Generated901.BaseClass1>::Method5()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Generated901.IBase1`1<class Generated901.BaseClass1>::Method6<object>()
		stelem.ref
		ldloc.s      actualResults
		call void [TestFramework]TestFramework::MethodCallTest(string,string,int32,string[])
		ret
	}
	.method static void M.IBase0<(Generated901.IBase0)W>(!!W inst, string exp) cil managed {
		.maxstack 9
		.locals init (string[] actualResults)
		ldc.i4.s    4
		newarr      string
		stloc.s     actualResults
		ldarg.1
		ldstr "M.IBase0<(Generated901.IBase0)W>(!!W inst, string exp)"
		ldc.i4.s 4
		ldloc.s      actualResults
		ldc.i4.s     0
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated901.IBase0::Method0()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     1
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated901.IBase0::Method1()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     2
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated901.IBase0::Method2<object>()
		stelem.ref
		ldloc.s      actualResults
		ldc.i4.s     3
		ldarga.s     0
		constrained. !!W
		callvirt     instance string Generated901.IBase0::Method3<object>()
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
		newobj instance void class Generated901.G3_C1373`1<class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod2150()
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method6<object>()
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method5()
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method4()
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G2_C400::Method7.MI.8556<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method4()
		ldstr "G2_C400::Method4.MI.8558()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method5()
		ldstr "G2_C400::Method5.MI.8560()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method6<object>()
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod4134<object>()
		ldstr "G3_C1373::ClassMethod4134.15825<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod4133()
		ldstr "G3_C1373::ClassMethod4133.15824()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method7<object>()
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod2150()
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method6<object>()
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method5()
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method4()
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated901.G3_C1373`1<class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod2150()
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method6<object>()
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method5()
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method4()
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G2_C400::Method7.MI.8556<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method4()
		ldstr "G2_C400::Method4.MI.8558()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method5()
		ldstr "G2_C400::Method5.MI.8560()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method6<object>()
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod4134<object>()
		ldstr "G3_C1373::ClassMethod4134.15825<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod4133()
		ldstr "G3_C1373::ClassMethod4133.15824()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method7<object>()
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod2150()
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method6<object>()
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method5()
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method4()
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1335<object>()
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1333()
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		callvirt instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1335<object>()
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1334<object>()
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1333()
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1332()
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method2<object>()
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method0()
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		callvirt instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string Generated901.IBase0::Method0()
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method1()
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method2<object>()
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		dup
		callvirt instance string Generated901.IBase0::Method3<object>()
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		pop
		ldloc.0
		dup
		callvirt instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
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
		newobj instance void class Generated901.G3_C1373`1<class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.A<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.A<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated901.Generated901::M.IBase0<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.B<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G2_C400.T.T<class Generated901.BaseClass1,class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G2_C400.B.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G2_C400.B.A<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C400::Method7.MI.8556<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass1,class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G2_C400::Method7.MI.8556<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.T<class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C400::Method7.MI.8556<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.B<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G2_C400::Method4.MI.8558()#G2_C400::Method5.MI.8560()#G2_C400::Method6.8561<System.Object>()#"
		call void Generated901.Generated901::M.IBase1.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G2_C400::Method4.MI.8558()#G2_C400::Method5.MI.8560()#G2_C400::Method6.8561<System.Object>()#"
		call void Generated901.Generated901::M.IBase1.A<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G3_C1373::ClassMethod4133.15824()#G3_C1373::ClassMethod4134.15825<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.G3_C1373.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G3_C1373::ClassMethod4133.15824()#G3_C1373::ClassMethod4134.15825<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.G3_C1373.A<class Generated901.G3_C1373`1<class Generated901.BaseClass0>>(!!0,string)
		newobj instance void class Generated901.G3_C1373`1<class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.A<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.A<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated901.Generated901::M.IBase0<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.B<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G2_C400.T.T<class Generated901.BaseClass1,class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G2_C400.B.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G2_C400::Method7.8555<System.Object>()#"
		call void Generated901.Generated901::M.G2_C400.B.A<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C400::Method7.MI.8556<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass1,class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G2_C400::Method7.MI.8556<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.T<class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C400::Method7.MI.8556<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.B<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G2_C400::Method4.MI.8558()#G2_C400::Method5.MI.8560()#G2_C400::Method6.8561<System.Object>()#"
		call void Generated901.Generated901::M.IBase1.T<class Generated901.BaseClass0,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G2_C400::Method4.MI.8558()#G2_C400::Method5.MI.8560()#G2_C400::Method6.8561<System.Object>()#"
		call void Generated901.Generated901::M.IBase1.A<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G3_C1373::ClassMethod4133.15824()#G3_C1373::ClassMethod4134.15825<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.G3_C1373.T<class Generated901.BaseClass1,class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G2_C400::ClassMethod1333.MI.8563()#G1_C8::ClassMethod1334.4831<System.Object>()#G2_C400::ClassMethod1335.MI.8564<System.Object>()#G3_C1373::ClassMethod2150.MI.15826()#G3_C1373::ClassMethod4133.15824()#G3_C1373::ClassMethod4134.15825<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G2_C400::Method4.8557()#G2_C400::Method5.8559()#G2_C400::Method6.8561<System.Object>()#G3_C1373::Method7.15823<System.Object>()#"
		call void Generated901.Generated901::M.G3_C1373.B<class Generated901.G3_C1373`1<class Generated901.BaseClass1>>(!!0,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.T<class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.A<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.A<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated901.Generated901::M.IBase0<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.B<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>>(!!0,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.A.B<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.B<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated901.Generated901::M.IBase0<class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>>(!!0,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.T.T<class Generated901.BaseClass1,class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.B.T<class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.B.A<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass1,class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.T<class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.A<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated901.Generated901::M.IBase0<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass0,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.A<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.B<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass1,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.B<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>>(!!0,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.T.T<class Generated901.BaseClass1,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.B.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::ClassMethod1332.4829()#G1_C8::ClassMethod1333.4830()#G1_C8::ClassMethod1334.4831<System.Object>()#G1_C8::ClassMethod1335.4832<System.Object>()#G1_C8::Method0.4823()#G1_C8::Method1.4825()#G1_C8::Method2.4826<System.Object>()#G1_C8::Method3.4828<System.Object>()#G1_C8::Method7.4821<System.Object>()#"
		call void Generated901.Generated901::M.G1_C8.B.B<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass1,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.B.B<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method0.MI.4824()#G1_C8::Method1.4825()#G1_C8::Method2.MI.4827<System.Object>()#G1_C8::Method3.4828<System.Object>()#"
		call void Generated901.Generated901::M.IBase0<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!0,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.T.T<class Generated901.BaseClass0,class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!2,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.T<class Generated901.BaseClass1,class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!1,string)
		ldloc.0
		ldstr "G1_C8::Method7.MI.4822<System.Object>()#"
		call void Generated901.Generated901::M.IBase2.A.B<class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>>(!!0,string)
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
		newobj instance void class Generated901.G3_C1373`1<class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod2150()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method6<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method5()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method4()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method7.MI.8556<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method4()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method4.MI.8558()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method5()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method5.MI.8560()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method6<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod4134<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G3_C1373::ClassMethod4134.15825<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod4133()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G3_C1373::ClassMethod4133.15824()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod2150()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method6<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method5()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method4()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated901.G3_C1373`1<class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod2150()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method6<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method5()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method4()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method7.8555<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G2_C400`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method7.MI.8556<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method4()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method4.MI.8558()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method5()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method5.MI.8560()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase1`1<class Generated901.BaseClass0>::Method6<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class IBase1`1<class Generated901.BaseClass0> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod4134<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G3_C1373::ClassMethod4134.15825<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod4133()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G3_C1373::ClassMethod4133.15824()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G3_C1373::Method7.15823<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod2150()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G3_C1373::ClassMethod2150.MI.15826()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method6<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method6.8561<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method5()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method5.8559()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method4()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::Method4.8557()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1335<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::ClassMethod1335.MI.8564<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1334<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1333()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G2_C400::ClassMethod1333.MI.8563()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::ClassMethod1332()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method3<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method2<object>()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method1()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G3_C1373`1<class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G3_C1373`1<class Generated901.BaseClass1>::Method0()
		calli default string(class Generated901.G3_C1373`1<class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G3_C1373`1<class Generated901.BaseClass1> on type class Generated901.G3_C1373`1<class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1335<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1334<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1333()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::ClassMethod1332()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass0,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1335<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1334<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1333()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::ClassMethod1332()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass0> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass0>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		newobj instance void class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::.ctor()
		stloc.0
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1335<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1335.4832<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1334<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1334.4831<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1333()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1333.4830()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::ClassMethod1332()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::ClassMethod1332.4829()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.4826<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.4823()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		castclass class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>
		ldloc.0
		ldvirtftn instance string class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method7.4821<System.Object>()"
		ldstr "class G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass1,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method0()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method0.MI.4824()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method1()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method1.4825()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method2<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method2.MI.4827<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string Generated901.IBase0::Method3<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method3.4828<System.Object>()"
		ldstr "IBase0 on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
		call void [TestFramework]TestFramework::MethodCallTest(string,string,string)
		ldloc.0
		ldloc.0
		ldvirtftn instance string class Generated901.IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1>::Method7<object>()
		calli default string(class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>)
		ldstr "G1_C8::Method7.MI.4822<System.Object>()"
		ldstr "class IBase2`2<class Generated901.BaseClass0,class Generated901.BaseClass1> on type class Generated901.G1_C8`2<class Generated901.BaseClass1,class Generated901.BaseClass1>"
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
		call void Generated901.Generated901::MethodCallingTest()
		call void Generated901.Generated901::ConstrainedCallsTest()
		call void Generated901.Generated901::StructConstrainedInterfaceCallsTest()
		call void Generated901.Generated901::CalliTest()
		ldc.i4 100
		ret
	}
}
